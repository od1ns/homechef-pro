using HomeChefPro.Application.Catalog.Ingredients.Dtos;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Application.Catalog.Ingredients.Mapping;

/// <summary>
/// Plain mapping functions (no AutoMapper) — small and explicit. Easy to unit test.
/// </summary>
public static class IngredientMapping
{
    public static IngredientDto ToDto(this Ingredient i) =>
        new(
            Id: i.Id,
            Name: i.Name,
            Description: i.Description,
            UseUnit: EnumDbMap<UseUnit>.ToDb(i.UseUnit),
            CurrentStockUseUnit: i.CurrentStockUseUnit,
            ReorderPointUseUnit: i.ReorderPointUseUnit,
            MinimumStockUseUnit: i.MinimumStockUseUnit,
            AvgCostPerUseUnitUsd: i.AvgCostPerUseUnitUsd,
            IsActive: i.IsActive,
            IsBelowReorderPoint: i.IsBelowReorderPoint,
            IsBelowMinimumStock: i.IsBelowMinimumStock,
            IsOutOfStock: i.IsOutOfStock,
            Presentations: i.Presentations.Select(ToDto).ToArray());

    public static IngredientPresentationDto ToDto(this IngredientPresentation p) =>
        new(
            Id: p.Id,
            Name: p.Name,
            PurchaseUnit: EnumDbMap<PurchaseUnit>.ToDb(p.PurchaseUnit),
            PurchaseQuantity: p.PurchaseQuantity,
            ConversionToUseUnit: p.ConversionToUseUnit,
            LastPurchasePriceUsd: p.LastPurchasePriceUsd,
            IsActive: p.IsActive);
}
