using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Orders;

public sealed class OrderItem : Entity<Guid>
{
    /// <summary>
    /// Pasada C / Fase 1C-A: tenant root. Default <c>Guid.Empty</c> (sentinel)
    /// hace que EF omita la columna en INSERT y la SQL DEFAULT inserte el
    /// piloto. Fase 2 reemplazara la sentinel por _currentChef.Id.
    /// </summary>
    public Guid ChefId { get; private set; }

    public Guid OrderId { get; private set; }
    public Guid DishId { get; private set; }
    public string DishNameSnapshot { get; private set; } = null!;

    /// <summary>
    /// Etapa 2: precio base del plato + suma de deltas de modificadores activos.
    /// unit_price_usd = recipe.selling_price + sum(modifier.qty * modifier.price_delta).
    /// </summary>
    public decimal UnitPriceUsd { get; private set; }
    public int Quantity { get; private set; }
    public decimal LineTotalUsd { get; private set; }
    public string? ItemNotes { get; private set; }
    public KitchenStatus KitchenStatus { get; private set; }
    public DateTimeOffset? PrepStartedAt { get; private set; }
    public DateTimeOffset? PrepCompletedAt { get; private set; }

    // Etapa 2: modificadores seleccionados por el cliente para este item.
    private readonly List<OrderItemModifier> _modifiers = [];
    public IReadOnlyList<OrderItemModifier> Modifiers => _modifiers.AsReadOnly();

    private OrderItem() { }

    private OrderItem(
        Guid id,
        Guid orderId,
        Guid dishId,
        string dishNameSnapshot,
        decimal unitPriceUsd,
        int quantity,
        decimal lineTotalUsd,
        string? itemNotes)
    {
        Id = id;
        OrderId = orderId;
        DishId = dishId;
        DishNameSnapshot = dishNameSnapshot;
        UnitPriceUsd = unitPriceUsd;
        Quantity = quantity;
        LineTotalUsd = lineTotalUsd;
        ItemNotes = itemNotes;
        KitchenStatus = KitchenStatus.Pending;
    }

    internal static OrderItem Create(
        Guid orderId,
        Guid dishId,
        string dishNameSnapshot,
        decimal unitPriceUsd,
        int quantity,
        string? itemNotes,
        Guid? id)
    {
        if (orderId == Guid.Empty) throw new DomainException("OrderId is required.");
        if (dishId == Guid.Empty) throw new DomainException("DishId is required.");
        if (string.IsNullOrWhiteSpace(dishNameSnapshot))
            throw new DomainException("Dish name snapshot is required.");
        if (dishNameSnapshot.Length > 200)
            throw new DomainException("Dish name snapshot must be at most 200 characters.");
        if (unitPriceUsd < 0) throw new DomainException("Unit price cannot be negative.");
        if (quantity <= 0) throw new DomainException("Quantity must be positive.");

        var lineTotal = decimal.Round(unitPriceUsd * quantity, 4, MidpointRounding.AwayFromZero);
        return new OrderItem(
            id ?? Guid.NewGuid(),
            orderId,
            dishId,
            dishNameSnapshot.Trim(),
            unitPriceUsd,
            quantity,
            lineTotal,
            string.IsNullOrWhiteSpace(itemNotes) ? null : itemNotes.Trim());
    }

    /// <summary>
    /// Etapa 2: agrega un snapshot de modificador al item ya creado.
    /// Llamar antes de insertar el item en DB.
    /// </summary>
    public void AddModifierSnapshot(
        Guid modifierId,
        string modifierName,
        int qty,
        decimal priceDelta)
    {
        var snap = OrderItemModifier.Create(
            orderItemId: Id,
            modifierId: modifierId,
            modifierNameSnapshot: modifierName,
            quantity: qty,
            priceDeltaUsdSnapshot: priceDelta);
        _modifiers.Add(snap);
    }

    internal void StartPrep(TimeProvider? clock)
    {
        if (KitchenStatus != KitchenStatus.Pending)
            throw new DomainException(
                $"Item {Id} cannot start prep from status '{KitchenStatus}'.");
        KitchenStatus = KitchenStatus.InPrep;
        PrepStartedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    internal void MarkReady(TimeProvider? clock)
    {
        if (KitchenStatus == KitchenStatus.Ready)
            return;
        if (KitchenStatus == KitchenStatus.Pending)
        {
            var now = (clock ?? TimeProvider.System).GetUtcNow();
            PrepStartedAt ??= now;
        }
        KitchenStatus = KitchenStatus.Ready;
        PrepCompletedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public bool IsReady => KitchenStatus == KitchenStatus.Ready;
}
