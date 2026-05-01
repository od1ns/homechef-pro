using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Ingredients;

public sealed class Ingredient : AggregateRoot<Guid>
{
    private readonly List<IngredientPresentation> _presentations = [];

    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public UseUnit UseUnit { get; private set; }

    public decimal CurrentStockUseUnit { get; private set; }
    public decimal ReorderPointUseUnit { get; private set; }
    public decimal MinimumStockUseUnit { get; private set; }
    public decimal AvgCostPerUseUnitUsd { get; private set; }

    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<IngredientPresentation> Presentations => _presentations.AsReadOnly();

    private Ingredient() { }

    private Ingredient(
        Guid id,
        string name,
        string? description,
        UseUnit useUnit,
        decimal reorderPoint,
        decimal minimumStock,
        DateTimeOffset now)
    {
        Id = id;
        Name = name;
        Description = description;
        UseUnit = useUnit;
        CurrentStockUseUnit = 0m;
        AvgCostPerUseUnitUsd = 0m;
        ReorderPointUseUnit = reorderPoint;
        MinimumStockUseUnit = minimumStock;
        IsActive = true;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Ingredient Create(
        string name,
        UseUnit useUnit,
        decimal reorderPoint = 0m,
        decimal minimumStock = 0m,
        string? description = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Ingredient name is required.");
        if (name.Length > 120)
            throw new DomainException("Ingredient name must be at most 120 characters.");
        if (reorderPoint < 0)
            throw new DomainException("Reorder point cannot be negative.");
        if (minimumStock < 0)
            throw new DomainException("Minimum stock cannot be negative.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new Ingredient(
            id ?? Guid.NewGuid(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            useUnit,
            reorderPoint,
            minimumStock,
            now);
    }

    public void Rename(string name, TimeProvider? clock = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Ingredient name is required.");
        if (name.Length > 120)
            throw new DomainException("Ingredient name must be at most 120 characters.");
        Name = name.Trim();
        Touch(clock);
    }

    public void UpdateReorderThresholds(decimal reorderPoint, decimal minimumStock, TimeProvider? clock = null)
    {
        if (reorderPoint < 0)
            throw new DomainException("Reorder point cannot be negative.");
        if (minimumStock < 0)
            throw new DomainException("Minimum stock cannot be negative.");
        ReorderPointUseUnit = reorderPoint;
        MinimumStockUseUnit = minimumStock;
        Touch(clock);
    }

    public void UpdateDescription(string? description, TimeProvider? clock = null)
    {
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch(clock);
    }

    public void Deactivate(TimeProvider? clock = null)
    {
        IsActive = false;
        Touch(clock);
    }

    public void Activate(TimeProvider? clock = null)
    {
        IsActive = true;
        Touch(clock);
    }

    public IngredientPresentation AddPresentation(
        string name,
        PurchaseUnit purchaseUnit,
        decimal purchaseQuantity,
        decimal conversionToUseUnit,
        decimal? lastPurchasePriceUsd = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (_presentations.Any(p => string.Equals(p.Name, name?.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Ingredient '{Name}' already has a presentation named '{name}'.");

        var presentation = IngredientPresentation.Create(
            ingredientId: Id,
            name: name!,
            purchaseUnit: purchaseUnit,
            purchaseQuantity: purchaseQuantity,
            conversionToUseUnit: conversionToUseUnit,
            lastPurchasePriceUsd: lastPurchasePriceUsd,
            clock: clock,
            id: id);

        _presentations.Add(presentation);
        Touch(clock);
        return presentation;
    }

    public bool IsBelowReorderPoint => CurrentStockUseUnit <= ReorderPointUseUnit;
    public bool IsBelowMinimumStock => CurrentStockUseUnit <= MinimumStockUseUnit;
    public bool IsOutOfStock => CurrentStockUseUnit <= 0m;

    /// <summary>
    /// Applied by Infrastructure after the SQL trigger updated the row (fn_apply_purchase_to_stock / waste).
    /// Domain does not recompute — it mirrors DB truth.
    /// </summary>
    internal void SyncStockFromDatabase(decimal currentStock, decimal avgCostPerUseUnitUsd, DateTimeOffset updatedAt)
    {
        CurrentStockUseUnit = currentStock;
        AvgCostPerUseUnitUsd = avgCostPerUseUnitUsd;
        UpdatedAt = updatedAt;
    }

    private void Touch(TimeProvider? clock) => UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
}
