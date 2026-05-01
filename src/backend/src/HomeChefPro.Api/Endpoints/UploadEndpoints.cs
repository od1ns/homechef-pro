using HomeChefPro.Application.Uploads.Abstractions;
using HomeChefPro.Infrastructure.Uploads;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace HomeChefPro.Api.Endpoints;

public static class UploadEndpoints
{
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/uploads")
            .WithTags("Uploads")
            .AllowAnonymous();

        // Anyone (including a guest customer who hasn't logged in) can upload a payment proof
        // image. This is intentional — the proof is associated with an order id when the
        // customer then calls POST /api/client/orders/{id}/payment with the returned URL.
        group.MapPost("payment-proofs", async (
            HttpRequest request,
            IFileStorage storage,
            IOptions<LocalFileStorageOptions> opts,
            CancellationToken ct) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest(new { error = "Expected multipart/form-data." });

            var form = await request.ReadFormAsync(ct).ConfigureAwait(false);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
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
        .DisableAntiforgery()
        .Accepts<IFormFile>("multipart/form-data");

        return app;
    }
}
