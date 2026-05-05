using System.Text.RegularExpressions;
using HomeChefPro.Application.Uploads.Abstractions;
using HomeChefPro.Infrastructure.Uploads;
using Microsoft.Extensions.Options;

namespace HomeChefPro.Api.Endpoints;

public static partial class UploadEndpoints
{
    // F-02 (audit Pasada A): validacion estricta del filename para servir comprobantes.
    // Solo aceptamos el formato que LocalFileStorage genera al subir:
    // 32 chars hex (Guid "N") + extension permitida.
    // Cualquier otro shape (path traversal "..", null bytes, unicode tricks) → no matchea → 404.
    [GeneratedRegex(@"^[a-f0-9]{32}\.(jpg|jpeg|png|webp)$", RegexOptions.IgnoreCase)]
    private static partial Regex SafePaymentProofFilename();

    // F-09 (Tier 2): validar magic bytes del upload. El cliente puede mandar un
    // body HTML/JS con Content-Type "image/jpeg"; el servidor debe rechazar.
    // Defense in depth — los archivos solo se sirven a Cashier/Admin (F-02),
    // pero igual no queremos guardar payloads ejecutables en disco.
    // No re-encodeamos (no se trae ImageSharp) — solo validamos firma.
    private static bool MagicBytesMatch(ReadOnlySpan<byte> buf, string contentType)
    {
        // JPEG: FF D8 FF
        if (contentType == "image/jpeg")
            return buf.Length >= 3 && buf[0] == 0xFF && buf[1] == 0xD8 && buf[2] == 0xFF;
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (contentType == "image/png")
            return buf.Length >= 8
                && buf[0] == 0x89 && buf[1] == 0x50 && buf[2] == 0x4E && buf[3] == 0x47
                && buf[4] == 0x0D && buf[5] == 0x0A && buf[6] == 0x1A && buf[7] == 0x0A;
        // WebP: "RIFF" .... "WEBP"
        if (contentType == "image/webp")
            return buf.Length >= 12
                && buf[0] == 0x52 && buf[1] == 0x49 && buf[2] == 0x46 && buf[3] == 0x46
                && buf[8] == 0x57 && buf[9] == 0x45 && buf[10] == 0x42 && buf[11] == 0x50;
        return false;
    }

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        // F-28 (Tier 2): rate limiting "upload" (20 req/min/IP) — bytes pesados,
        // y POST anonymous de payment-proofs es vector de abuso storage.
        var group = app.MapGroup("/api/uploads").WithTags("Uploads")
            .RequireRateLimiting("upload");

        // ----- POST: subir comprobante (anonymous, intencional para guests) -----
        // Anyone (including a guest customer who hasn't logged in) can upload a payment proof
        // image. The proof is then associated with an order id when the customer calls
        // POST /api/client/orders/{id}/payment with the returned URL.
        // F-04 + F-09 cerrados en Tier 1/2. F-10 (re-encode con stripper EXIF) queda pendiente para Tier 3.
        group.MapPost("payment-proofs", async (
            HttpRequest request,
            IFileStorage storage,
            IOptions<LocalFileStorageOptions> opts,
            HomeChefPro.Application.Abstractions.IHomeChefProDbContext db,
            TimeProvider clock,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart/form-data." });

            var form = await request.ReadFormAsync(ct).ConfigureAwait(false);
            var file = form.Files.GetFile("file") ?? (form.Files.Count > 0 ? form.Files[0] : null);
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded under field 'file'." });

            if (file.Length > opts.Value.MaxBytes)
                return Results.BadRequest(new
                {
                    error = $"File exceeds limit of {opts.Value.MaxBytes / 1024} KB.",
                });

            var contentType = file.ContentType?.ToLowerInvariant();
            if (string.IsNullOrEmpty(contentType)
                || !opts.Value.AllowedContentTypes.Contains(contentType))
            {
                return Results.BadRequest(new
                {
                    error = $"Content-Type '{contentType}' is not allowed.",
                    allowed = opts.Value.AllowedContentTypes,
                });
            }

            // F-09 (Tier 2): inspeccionar primeros 12 bytes y validar firma.
            // Lectura directa del Stream del IFormFile.
            byte[] header = new byte[12];
            int read;
            await using (var probe = file.OpenReadStream())
            {
                read = 0;
                while (read < header.Length)
                {
                    var n = await probe.ReadAsync(header.AsMemory(read, header.Length - read), ct).ConfigureAwait(false);
                    if (n == 0) break;
                    read += n;
                }
            }
            if (read < 3 || !MagicBytesMatch(header.AsSpan(0, read), contentType))
            {
                return Results.BadRequest(new
                {
                    error = "File contents do not match a supported image format (JPEG/PNG/WebP).",
                });
            }

            var ext = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(ext))
                ext = contentType switch
                {
                    "image/jpeg" => ".jpg",
                    "image/png"  => ".png",
                    "image/webp" => ".webp",
                    _            => ".bin",
                };

            var filename = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
            await using var stream = file.OpenReadStream();
            var result = await storage.SaveAsync(
                folder: "payment-proofs",
                filename: filename,
                content: stream,
                contentType: contentType,
                ct: ct).ConfigureAwait(false);

            // F-23 (audit Pasada B): persistir el handle del upload. El cliente recibe
            // el id y debe enviarlo como `proofImageId` al hacer POST de payment.
            // Asi evitamos que envie URLs externas o reuse comprobantes ya aprobados.
            var upload = HomeChefPro.Domain.Payments.PaymentProofUpload.Create(
                filename: filename,
                contentType: contentType!,
                sizeBytes: result.SizeBytes,
                clock: clock);
            db.PaymentProofUploads.Add(upload);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return Results.Ok(new
            {
                id = upload.Id,             // F-23: el cliente lo envia como proofImageId
                url = result.Url,           // todavia retornamos url para preview en UI
                contentType = result.ContentType,
                sizeBytes = result.SizeBytes,
            });
        })
        .AllowAnonymous()
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data")
        .WithName("UploadPaymentProof");

        // ----- GET: servir comprobante (autenticado, solo Cashier/Admin) -----
        // F-02: reemplaza el UseStaticFiles que servia /uploads/* sin auth.
        // Politica de acceso conservadora: solo roles que aprueban pagos pueden ver el comprobante.
        // Si en el futuro el cliente debe ver su propio comprobante, agregar verificacion de
        // ownership por order_id consultando la tabla payments.
        group.MapGet("payment-proofs/{filename}", (
            string filename,
            IOptions<LocalFileStorageOptions> opts,
            HttpResponse response) =>
        {
            // Bloqueo total de path traversal y nombres no-canonicos.
            if (string.IsNullOrEmpty(filename) || !SafePaymentProofFilename().IsMatch(filename))
                return Results.NotFound();

            var path = Path.Combine(opts.Value.LocalRoot, "payment-proofs", filename);
            // Defense in depth: el path canonicalizado debe seguir adentro del directorio esperado.
            var fullPath = Path.GetFullPath(path);
            var fullDir = Path.GetFullPath(Path.Combine(opts.Value.LocalRoot, "payment-proofs"));
            if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !string.Equals(fullPath, fullDir, StringComparison.Ordinal))
            {
                return Results.NotFound();
            }

            if (!File.Exists(fullPath))
                return Results.NotFound();

            var contentType = Path.GetExtension(filename).ToLowerInvariant() switch
            {
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png"  => "image/png",
                ".webp" => "image/webp",
                _       => "application/octet-stream",
            };

            // Headers de seguridad por defense in depth (F-05 cubre los globales).
            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Cache-Control"] = "private, no-store";
            response.Headers["Content-Disposition"] = $"inline; filename=\"{filename}\"";

            return Results.File(fullPath, contentType, enableRangeProcessing: false);
        })
        .RequireAuthorization("Cashier")
        .WithName("GetPaymentProof");

        return app;
    }
}
