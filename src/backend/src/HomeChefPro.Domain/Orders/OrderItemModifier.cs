using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

/// <summary>
/// Etapa 2: snapshot inmutable del modificador elegido por el cliente
/// al momento del pedido. Parte del historial audit-trail de order_items.
/// </summary>
public sealed class OrderItemModifier : Entity<Guid>
{
    public Guid OrderItemId { get; private set; }
    public Guid ModifierId { get; private set; }

    /// <summary>Nombre del modificador al momento del pedido (inmutable).</summary>
    public string ModifierNameSnapshot { get; private set; } = null!;

    /// <summary>Cantidad elegida por el cliente (puede ser 0 si el chef define min=0).</summary>
    public int Quantity { get; private set; }

    /// <summary>Delta de precio por unidad al momento del pedido (inmutable).</summary>
    public decimal PriceDeltaUsdSnapshot { get; private set; }

    /// <summary>Quantity * PriceDeltaUsdSnapshot, precalculado.</summary>
    public decimal LineDeltaUsd { get; private set; }

    private OrderItemModifier() { }

    internal static OrderItemModifier Create(
        Guid orderItemId,
        Guid modifierId,
        string modifierNameSnapshot,
        int quantity,
        decimal priceDeltaUsdSnapshot,
        Guid? id = null)
    {
        if (orderItemId == Guid.Empty)
            throw new DomainException("OrderItemId es requerido.");
        if (modifierId == Guid.Empty)
            throw new DomainException("ModifierId es requerido.");
        if (string.IsNullOrWhiteSpace(modifierNameSnapshot))
            throw new DomainException("El snapshot del nombre es requerido.");
        if (quantity < 0)
            throw new DomainException("La cantidad del modificador no puede ser negativa.");

        return new OrderItemModifier
        {
            Id = id ?? Guid.NewGuid(),
            OrderItemId = orderItemId,
            ModifierId = modifierId,
            ModifierNameSnapshot = modifierNameSnapshot.Trim(),
            Quantity = quantity,
            PriceDeltaUsdSnapshot = priceDeltaUsdSnapshot,
            LineDeltaUsd = decimal.Round(quantity * priceDeltaUsdSnapshot, 4, MidpointRounding.AwayFromZero),
        };
    }
}
