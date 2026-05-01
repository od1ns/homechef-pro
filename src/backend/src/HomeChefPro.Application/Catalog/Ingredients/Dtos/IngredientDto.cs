namespace HomeChefPro.Application.Catalog.Ingredients.Dtos;

public sealed record IngredientDto(
    Guid Id,
    string Name,
    string? Description,
    string UseUnit,
    decimal CurrentStockUseUnit,
    decimal ReorderPointUseUnit,
    decimal MinimumStockUseUnit,
    decimal AvgCostPerUseUnitUsd,
    bool IsActive,
    bool IsBelowReorderPoint,
    bool IsBelowMinimumStock,
    bool IsOutOfStock,
    IReadOnlyList<IngredientPresentationDto> Presentations);

public sealed record IngredientPresentationDto(
    Guid Id,
    string Name,
    string PurchaseUnit,
    decimal PurchaseQuantity,
    decimal ConversionToUseUnit,
    decimal? LastPurchasePriceUsd,
    bool IsActive);
