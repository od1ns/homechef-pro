namespace HomeChefPro.Application.Catalog.Recipes.Dtos;

public sealed record RecipeCostDto(
    Guid RecipeId,
    string RecipeName,
    bool IsSubRecipe,
    decimal TotalCostUsd,
    decimal? YieldQuantity,
    string? YieldUnit,
    decimal? CostPerYieldUnit,
    IReadOnlyList<RecipeCostLineDto> Lines);

public sealed record RecipeCostLineDto(
    string Kind,                      // "ingredient" | "sub_recipe"
    Guid RefId,
    string RefName,
    decimal Quantity,
    string UnitLabel,                 // use_unit for ingredient, yield_unit for sub
    decimal UnitCostUsd,
    decimal LineCostUsd,
    RecipeCostDto? SubBreakdown);
