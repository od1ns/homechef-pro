namespace HomeChefPro.Domain.Catalog.Recipes.Services;

public sealed record RecipeCostBreakdown(
    Guid RecipeId,
    string RecipeName,
    bool IsSubRecipe,
    decimal TotalCostUsd,
    decimal? YieldQuantity,
    string? YieldUnit,
    decimal? CostPerYieldUnit,
    IReadOnlyList<CostLine> Lines);
