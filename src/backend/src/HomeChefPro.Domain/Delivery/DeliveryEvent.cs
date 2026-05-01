using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Delivery;

public sealed class DeliveryEvent : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string? ExternalTrackingId { get; private set; }
    public DeliveryStatus NormalizedStatus { get; private set; }
    public string? RawStatus { get; private set; }
    public string RawPayloadJson { get; private set; } = null!;
    public string? Signature { get; private set; }
    public bool? SignatureValid { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    private DeliveryEvent() { }

    public static DeliveryEvent Record(
        Guid orderId,
        string provider,
        DeliveryStatus normalizedStatus,
        string rawPayloadJson,
        string? externalTrackingId = null,
        string? rawStatus = null,
        string? signature = null,
        bool? signatureValid = null,
        DateTimeOffset? receivedAt = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException("Provider is required.");
        if (string.IsNullOrWhiteSpace(rawPayloadJson))
            throw new DomainException("Raw payload is required.");

        var when = receivedAt ?? (clock ?? TimeProvider.System).GetUtcNow();
        return new DeliveryEvent
        {
            Id = id ?? Guid.NewGuid(),
            OrderId = orderId,
            Provider = provider.Trim(),
            ExternalTrackingId = string.IsNullOrWhiteSpace(externalTrackingId) ? null : externalTrackingId.Trim(),
            NormalizedStatus = normalizedStatus,
            RawStatus = string.IsNullOrWhiteSpace(rawStatus) ? null : rawStatus.Trim(),
            RawPayloadJson = rawPayloadJson,
            Signature = string.IsNullOrWhiteSpace(signature) ? null : signature,
            SignatureValid = signatureValid,
            ReceivedAt = when,
        };
    }
}
