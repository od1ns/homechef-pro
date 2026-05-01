using HomeChefPro.Application.Catalog.Recipes.Dtos;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Catalog.Recipes.Services;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Application.Catalog.Recipes.Mapping;

public static class RecipeMapping
{
    public static RecipeDto ToDto(this Recipe r) =>
        new(
            Id: r.Id,
            Name: r.Name,
            Description: r.Description,
            Category: r.Category,
            IsSubRecipe: r.IsSubRecipe,
            ProcedureMarkdown: r.ProcedureMarkdown,
            YieldQuantity: r.YieldQuantity,
            YieldUnit: r.YieldUnit is null ? null : EnumDbMap<YieldUnit>.ToDb(r.YieldUnit.Value),
            SuggestedPriceUsd: r.SuggestedPriceUsd,
            SellingPriceUsd: r.SellingPriceUsd,
            PrepTimeMinutes: r.PrepTimeMinutes,
            ImageUrl: r.ImageUrl,
            IsActive: r.IsActive,
            IsOutOfStock: r.IsOutOfStock,
            MenuType: EnumDbMap<MenuType>.ToDb(r.MenuType),
            SpecialFrom: r.SpecialFrom,
            SpecialTo: r.SpecialTo,
            Components: r.Components.OrderBy(c => c.DisplayOrder).Select(ToDto).ToArray());

    public static RecipeComponentDto ToDto(this RecipeComponent c) =>
        new(
            Id: c.Id,
            IngredientId: c.IngredientId,
            SubRecipeId: c.SubRecipeId,
            Quantity: c.Quantity,
            Notes: c.Notes,
            DisplayOrder: c.DisplayOrder);

    public static RecipeSummaryDto ToSummary(this Recipe r) =>
        new(
            Id: r.Id,
            Name: r.Name,
            Category: r.Category,
            IsSubRecipe: r.IsSubRecipe,
            SellingPriceUsd: r.SellingPriceUsd,
            PrepTimeMinutes: r.PrepTimeMinutes,
            ImageUrl: r.ImageUrl,
            IsActive: r.IsActive,
            IsOutOfStock: r.IsOutOfStock,
            MenuType: EnumDbMap<MenuType>.ToDb(r.MenuType));

    public static RecipeCostDto ToDto(this RecipeCostBreakdown b) =>
        new(
            RecipeId: b.RecipeId,
            RecipeName: b.RecipeName,
            IsSubRecipe: b.IsSubRecipe,
            TotalCostUsd: b.TotalCostUsd,
            YieldQuantity: b.YieldQuantity,
            YieldUnit: b.YieldUnit,
            CostPerYieldUnit: b.CostPerYieldUnit,
            Lines: b.Lines.Select(ToLineDto).ToArray());

    private static RecipeCostLineDto ToLineDto(CostLine line) => line switch
    {
        IngredientCostLine i => new RecipeCostLineDto(
            Kind: "ingredient",
            RefId: i.IngredientId,
            RefName: i.IngredientName,
            Quantity: i.Quantity,
            UnitLabel: i.UseUnit,
            UnitCostUsd: i.UnitCostUsd,
            LineCostUsd: i.LineCostUsd,
            SubBreakdown: null),
        SubRecipeCostLine s => new RecipeCostLineDto(
            Kind: "sub_recipe",
            RefId: s.SubRecipeId,
            RefName: s.SubRecipeName,
            Quantity: s.Quantity,
            UnitLabel: s.YieldUnit,
            UnitCostUsd: s.UnitCostUsd,
            LineCostUsd: s.LineCostUsd,
            SubBreakdown: s.SubBreakdown.ToDto()),
        _ => throw new InvalidOperationException($"Unknown CostLine type: {line.GetType().Name}"),
    };
}
