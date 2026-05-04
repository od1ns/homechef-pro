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

    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uploads").WithTags("Uploads");

        // ----- POST: subir comprobante (anonymous, intencional para guests) -----
        // Anyone (including a guest customer who hasn't logged in) can upload a payment proof
        // image. The proof is then associated with an order id when the customer calls
        // POST /api/client/orders/{id}/payment with the returned URL.
        // TODO (F-04, F-10): aplicar rate limiting al subir; agregar magic-byte + re-encode (F-09).
        group.MapPost("payment-proofs", async (
            HttpRequest request,
            IFileStorage storage,
            IOptions<LocalFileStorageOptions> opts,
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

            return Results.Ok(new
            {
                url = result.Url,
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
