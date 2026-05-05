using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Inventory;

public sealed class IngredientPurchase : AggregateRoot<Guid>
{
    /// <summary>
    /// Pasada C / Fase 1C-A: tenant root. Default <c>Guid.Empty</c> (sentinel)
    /// hace que EF omita la columna en INSERT y la SQL DEFAULT inserte el
    /// piloto. Fase 2 reemplazara la sentinel por _currentChef.Id.
    /// </summary>
    public Guid ChefId { get; private set; }

    public Guid IngredientId { get; private set; }
    public Guid PresentationId { get; private set; }
    public decimal QuantityPurchased { get; private set; }
    public decimal UnitPriceUsd { get; private set; }
    public decimal TotalCostUsd { get; private set; }
    public string? Supplier { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset PurchasedAt { get; private set; }
    public Guid RecordedBy { get; private set; }
    public string? Notes { get; private set; }

    private IngredientPurchase() { }

    private IngredientPurchase(
        Guid id,
        Guid ingredientId,
        Guid presentationId,
        decimal quantityPurchased,
        decimal unitPriceUsd,
        decimal totalCostUsd,
        string? supplier,
        string? reference,
        DateTimeOffset purchasedAt,
        Guid recordedBy,
        string? notes)
    {
        Id = id;
        IngredientId = ingredientId;
        PresentationId = presentationId;
        QuantityPurchased = quantityPurchased;
        UnitPriceUsd = unitPriceUsd;
        TotalCostUsd = totalCostUsd;
        Supplier = supplier;
        Reference = reference;
        PurchasedAt = purchasedAt;
        RecordedBy = recordedBy;
        Notes = notes;
    }

    public static IngredientPurchase Record(
        Guid ingredientId,
        Guid presentationId,
        decimal quantityPurchased,
        decimal unitPriceUsd,
        Guid recordedBy,
        string? supplier = null,
        string? reference = null,
        string? notes = null,
        DateTimeOffset? purchasedAt = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (ingredientId == Guid.Empty)
            throw new DomainException("IngredientId is required.");
        if (presentationId == Guid.Empty)
            throw new DomainException("PresentationId is required.");
        if (recordedBy == Guid.Empty)
            throw new DomainException("RecordedBy is required.");
        if (quantityPurchased <= 0)
            throw new DomainException("Quantity purchased must be positive.");
        if (unitPriceUsd <= 0)
            throw new DomainException("Unit price must be positive.");

        var total = decimal.Round(quantityPurchased * unitPriceUsd, 4, MidpointRounding.AwayFromZero);
        if (total <= 0)
            throw new DomainException("Total cost must be positive.");

        var when = purchasedAt ?? (clock ?? TimeProvider.System).GetUtcNow();
        return new IngredientPurchase(
            id ?? Guid.NewGuid(),
            ingredientId,
            presentationId,
            quantityPurchased,
            unitPriceUsd,
            total,
            string.IsNullOrWhiteSpace(supplier) ? null : supplier.Trim(),
            string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
            when,
            recordedBy,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());
    }
}
