using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Ingredients;

public sealed class IngredientPresentation : Entity<Guid>
{
    public Guid IngredientId { get; private set; }
    public string Name { get; private set; } = null!;
    public PurchaseUnit PurchaseUnit { get; private set; }
    public decimal PurchaseQuantity { get; private set; }
    public decimal ConversionToUseUnit { get; private set; }
    public decimal? LastPurchasePriceUsd { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private IngredientPresentation() { }

    private IngredientPresentation(
        Guid id,
        Guid ingredientId,
        string name,
        PurchaseUnit purchaseUnit,
        decimal purchaseQuantity,
        decimal conversionToUseUnit,
        decimal? lastPurchasePriceUsd,
        DateTimeOffset now)
    {
        Id = id;
        IngredientId = ingredientId;
        Name = name;
        PurchaseUnit = purchaseUnit;
        PurchaseQuantity = purchaseQuantity;
        ConversionToUseUnit = conversionToUseUnit;
        LastPurchasePriceUsd = lastPurchasePriceUsd;
        IsActive = true;
        CreatedAt = now;
        UpdatedAt = now;
    }

    internal static IngredientPresentation Create(
        Guid ingredientId,
        string name,
        PurchaseUnit purchaseUnit,
        decimal purchaseQuantity,
        decimal conversionToUseUnit,
        decimal? lastPurchasePriceUsd = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Presentation name is required.");
        if (name.Length > 120)
            throw new DomainException("Presentation name must be at most 120 characters.");
        if (purchaseQuantity <= 0)
            throw new DomainException("Purchase quantity must be positive.");
        if (conversionToUseUnit <= 0)
            throw new DomainException("Conversion to use unit must be positive.");
        if (lastPurchasePriceUsd is < 0)
            throw new DomainException("Last purchase price cannot be negative.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new IngredientPresentation(
            id ?? Guid.NewGuid(),
            ingredientId,
            name.Trim(),
            purchaseUnit,
            purchaseQuantity,
            conversionToUseUnit,
            lastPurchasePriceUsd,
            now);
    }

    /// <summary>
    /// Total use-unit quantity produced by buying <paramref name="quantityPurchased"/> of this presentation.
    /// Matches the trigger formula: quantityPurchased * purchaseQuantity * conversionToUseUnit.
    /// </summary>
    public decimal ToUseUnits(decimal quantityPurchased) =>
        quantityPurchased * PurchaseQuantity * ConversionToUseUnit;

    public void UpdateConversion(decimal purchaseQuantity, decimal conversionToUseUnit, TimeProvider? clock = null)
    {
        if (purchaseQuantity <= 0)
            throw new DomainException("Purchase quantity must be positive.");
        if (conversionToUseUnit <= 0)
            throw new DomainException("Conversion to use unit must be positive.");
        PurchaseQuantity = purchaseQuantity;
        ConversionToUseUnit = conversionToUseUnit;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    internal void RecordLastPurchasePrice(decimal priceUsd, TimeProvider? clock = null)
    {
        if (priceUsd < 0)
            throw new DomainException("Last purchase price cannot be negative.");
        LastPurchasePriceUsd = priceUsd;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public void Deactivate(TimeProvider? clock = null)
    {
        IsActive = false;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public void Activate(TimeProvider? clock = null)
    {
        IsActive = true;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }
}
