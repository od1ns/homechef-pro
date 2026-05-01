using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Delivery;

public sealed class DeliveryTracking : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public string Provider { get; private set; } = null!;
    public string? ExternalTrackingId { get; private set; }
    public DeliveryStatus CurrentStatus { get; private set; }

    public string? CourierName { get; private set; }
    public string? CourierPhone { get; private set; }
    public string? CourierVehicle { get; private set; }

    public decimal? LastKnownLat { get; private set; }
    public decimal? LastKnownLng { get; private set; }
    public DateTimeOffset? LastEventAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private DeliveryTracking() { }

    private DeliveryTracking(
        Guid id,
        Guid orderId,
        string provider,
        string? externalTrackingId,
        DateTimeOffset now)
    {
        Id = id;
        OrderId = orderId;
        Provider = provider;
        ExternalTrackingId = externalTrackingId;
        CurrentStatus = DeliveryStatus.Assigned;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static DeliveryTracking Assign(
        Guid orderId,
        string provider,
        string? externalTrackingId = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException("Provider is required.");
        if (provider.Length > 60)
            throw new DomainException("Provider must be at most 60 characters.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new DeliveryTracking(
            id ?? Guid.NewGuid(),
            orderId,
            provider.Trim(),
            string.IsNullOrWhiteSpace(externalTrackingId) ? null : externalTrackingId.Trim(),
            now);
    }

    public void UpdateCourier(string? name, string? phone, string? vehicle, TimeProvider? clock = null)
    {
        CourierName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        CourierPhone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        CourierVehicle = string.IsNullOrWhiteSpace(vehicle) ? null : vehicle.Trim();
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public void ApplyEvent(
        DeliveryStatus status,
        decimal? lat = null,
        decimal? lng = null,
        DateTimeOffset? eventAt = null,
        TimeProvider? clock = null)
    {
        var now = (clock ?? TimeProvider.System).GetUtcNow();
        CurrentStatus = status;
        if (lat is not null) LastKnownLat = lat;
        if (lng is not null) LastKnownLng = lng;
        LastEventAt = eventAt ?? now;
        UpdatedAt = now;
    }
}
