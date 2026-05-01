using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Delivery;

namespace HomeChefPro.Application.Delivery.Services;

/// <summary>
/// Normalizes provider-specific raw status strings into our internal <see cref="DeliveryStatus"/>.
/// Providers add their own mappings here as we integrate them.
/// </summary>
public static class DeliveryStatusMap
{
    // Lowercased-raw → normalized
    private static readonly Dictionary<string, Dictionary<string, DeliveryStatus>> _byProvider = new(StringComparer.OrdinalIgnoreCase)
    {
        ["yummy"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["pending"]      = DeliveryStatus.Assigned,
            ["assigned"]     = DeliveryStatus.Assigned,
            ["picked_up"]    = DeliveryStatus.PickedUp,
            ["in_transit"]   = DeliveryStatus.OnTheWay,
            ["on_the_way"]   = DeliveryStatus.OnTheWay,
            ["delivered"]    = DeliveryStatus.Delivered,
            ["cancelled"]    = DeliveryStatus.Cancelled,
            ["failed"]       = DeliveryStatus.Failed,
        },
        ["ridery"] = new(StringComparer.OrdinalIgnoreCase)
        {
            ["courier_assigned"] = DeliveryStatus.Assigned,
            ["pickup_done"]      = DeliveryStatus.PickedUp,
            ["en_route"]         = DeliveryStatus.OnTheWay,
            ["completed"]        = DeliveryStatus.Delivered,
            ["cancelled"]        = DeliveryStatus.Cancelled,
            ["failed"]           = DeliveryStatus.Failed,
        },
    };

    public static DeliveryStatus Normalize(string provider, string rawStatus)
    {
        if (string.IsNullOrWhiteSpace(rawStatus))
            return DeliveryStatus.Unknown;

        // 1. Try provider-specific map.
        if (_byProvider.TryGetValue(provider, out var map)
            && map.TryGetValue(rawStatus.Trim(), out var mapped))
            return mapped;

        // 2. Try our own enum values as-is (covers 'manual' provider and our test fixtures).
        if (EnumDbMap<DeliveryStatus>.TryFromDb(rawStatus.Trim().ToLowerInvariant(), out var direct))
            return direct;

        // 3. Fallback.
        return DeliveryStatus.Unknown;
    }
}
