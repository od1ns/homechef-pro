using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Payments;

/// <summary>
/// F-23 (audit Pasada B): handle de un upload de comprobante. Cada
/// <c>POST /api/uploads/payment-proofs</c> crea un row aqui. El cliente
/// recibe el <see cref="Entity{TId}.Id"/> y lo envia como <c>proofImageId</c> al hacer
/// <c>POST /api/client/orders/{id}/payment</c>. El handler de
/// SubmitPaymentProof valida que el id existe y no esta reclamado, y al
/// validar lo marca como reclamado (anti-reuse).
/// </summary>
public sealed class PaymentProofUpload : Entity<Guid>
{
    public string Filename { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public DateTimeOffset UploadedAt { get; private set; }
    public DateTimeOffset? ClaimedAt { get; private set; }
    public Guid? ClaimedByPaymentId { get; private set; }

    // EF Core needs a parameterless ctor.
    private PaymentProofUpload() : base() { }

    private PaymentProofUpload(
        Guid id, string filename, string contentType, long sizeBytes, TimeProvider clock)
        : base(id)
    {
        Filename = filename;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = clock.GetUtcNow();
    }

    public static PaymentProofUpload Create(
        string filename, string contentType, long sizeBytes, TimeProvider clock)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new DomainException("Filename is required.");
        if (string.IsNullOrWhiteSpace(contentType))
            throw new DomainException("ContentType is required.");
        if (sizeBytes <= 0)
            throw new DomainException("SizeBytes must be positive.");

        return new PaymentProofUpload(Guid.NewGuid(), filename, contentType, sizeBytes, clock);
    }

    /// <summary>
    /// Marca el upload como reclamado por un Payment. Tira si ya estaba reclamado.
    /// </summary>
    public void Claim(Guid paymentId, TimeProvider clock)
    {
        if (ClaimedAt is not null)
            throw new DomainException(
                $"Upload {Id} already claimed by payment {ClaimedByPaymentId} at {ClaimedAt:o}.");
        ClaimedAt = clock.GetUtcNow();
        ClaimedByPaymentId = paymentId;
    }
}
