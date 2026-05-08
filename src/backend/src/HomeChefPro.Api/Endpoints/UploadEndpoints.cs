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

    // Etapa 1: regex para imagenes de recipe (mismo formato que payment proofs).
    [GeneratedRegex(@"^[a-f0-9]{32}\.(jpg|jpeg|png|webp)$", RegexOptions.IgnoreCase)]
    private static partial Regex SafeRecipeImageFilename();

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
            // Pasada C / H-05: en single-tenant el upload anonimo va al piloto.
            // Fase 2: tomar de currentUser.ChefId si esta autenticado, o del
            // header del request publico (ej. X-Chef-Id derivado del subdominio).
            var chefId = HomeChefPro.Domain.Tenancy.Chef.PilotoId;
            var result = await storage.SaveAsync(
                chefId: chefId,
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
        // Pasada C / H-05: route ahora incluye chef_id como prefix.
        // El chefId viene como Guid (con guiones) por convencion de Minimal API.
        group.MapGet("{chefId:guid}/payment-proofs/{filename}", (
            Guid chefId,
            string filename,
            IOptions<LocalFileStorageOptions> opts,
            HttpResponse response) =>
        {
            // Bloqueo total de path traversal y nombres no-canonicos.
            if (string.IsNullOrEmpty(filename) || !SafePaymentProofFilename().IsMatch(filename))
                return Results.NotFound();

            var chefIdStr = chefId.ToString("N");
            var path = Path.Combine(opts.Value.LocalRoot, chefIdStr, "payment-proofs", filename);
            // Defense in depth: el path canonicalizado debe seguir adentro del directorio esperado.
            var fullPath = Path.GetFullPath(path);
            var fullDir = Path.GetFullPath(Path.Combine(opts.Value.LocalRoot, chefIdStr, "payment-proofs"));
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

        // Etapa 1: GET publico anonimo de imagenes de recipes.
        // El menu del cliente es anonymous, asi que estas imagenes son
        // publicamente accesibles. Aplicamos misma defense-in-depth de path
        // traversal que F-02 (regex strict + canonical path check).
        group.MapGet("{chefId:guid}/recipes/{filename}", (
            Guid chefId,
            string filename,
            IOptions<LocalFileStorageOptions> opts,
            HttpResponse response) =>
        {
            if (string.IsNullOrEmpty(filename) || !SafeRecipeImageFilename().IsMatch(filename))
                return Results.NotFound();

            var chefIdStr = chefId.ToString("N");
            var path = Path.Combine(opts.Value.LocalRoot, chefIdStr, "recipes", filename);
            var fullPath = Path.GetFullPath(path);
            var fullDir = Path.GetFullPath(Path.Combine(opts.Value.LocalRoot, chefIdStr, "recipes"));
            if (!fullPath.StartsWith(fullDir + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && !string.Equals(fullPath, fullDir, StringComparison.Ordinal))
                return Results.NotFound();

            if (!File.Exists(fullPath))
                return Results.NotFound();

            var contentType = Path.GetExtension(filename).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream",
            };

            response.Headers["X-Content-Type-Options"] = "nosniff";
            response.Headers["Cache-Control"] = "public, max-age=86400";  // 1 dia (recipes no cambian seguido)
            return Results.File(fullPath, contentType, enableRangeProcessing: false);
        })
        .AllowAnonymous()
        .WithName("GetRecipeImage");

        return app;
    }
}
