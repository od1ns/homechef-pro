using HomeChefPro.Application.Uploads.Abstractions;
using Microsoft.Extensions.Options;

namespace HomeChefPro.Infrastructure.Uploads;

/// <summary>
/// F-23 (audit Pasada B): implementacion de <see cref="IUploadUrlBuilder"/> que construye
/// la URL desde <see cref="LocalFileStorageOptions.PublicBaseUrl"/>.
/// </summary>
public sealed class UploadUrlBuilder(IOptions<LocalFileStorageOptions> options) : IUploadUrlBuilder
{
    private readonly string _publicBase = options.Value.PublicBaseUrl.TrimEnd('/');

    public string BuildPaymentProofUrl(Guid chefId, string filename)
    {
        if (chefId == Guid.Empty)
            throw new ArgumentException("chefId required", nameof(chefId));
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("filename required", nameof(filename));
        // Pasada C / H-05: chef_id como prefix de path.
        return $"{_publicBase}/{chefId:N}/payment-proofs/{filename}";
    }

    public string BuildRecipeImageUrl(Guid chefId, string filename)
    {
        if (chefId == Guid.Empty)
            throw new ArgumentException("chefId required", nameof(chefId));
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("filename required", nameof(filename));
        // Etapa 1: imagenes de recipes en folder "recipes/" per-chef.
        return $"{_publicBase}/{chefId:N}/recipes/{filename}";
    }
}
