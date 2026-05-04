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

    public string BuildPaymentProofUrl(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("filename required", nameof(filename));
        return $"{_publicBase}/payment-proofs/{filename}";
    }
}
