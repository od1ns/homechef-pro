namespace HomeChefPro.Application.Uploads.Abstractions;

/// <summary>
/// F-23 (audit Pasada B): construye la URL canonica de un comprobante a partir del filename.
/// Application no conoce la implementacion (no referencia Infrastructure); el handler de
/// SubmitPaymentProof inyecta esto para no acoplarse a LocalFileStorageOptions ni a IConfiguration.
/// </summary>
public interface IUploadUrlBuilder
{
    /// <param name="chefId">Pasada C / H-05: tenant del archivo. URL queda
    /// como <c>{publicBase}/{chefId}/payment-proofs/{filename}</c>.</param>
    string BuildPaymentProofUrl(Guid chefId, string filename);

    /// <summary>Etapa 1: URL canonica de la imagen de un recipe.</summary>
    string BuildRecipeImageUrl(Guid chefId, string filename);
}
