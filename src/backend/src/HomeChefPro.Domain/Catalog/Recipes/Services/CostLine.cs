namespace HomeChefPro.Domain.Catalog.Recipes.Services;

public abstract record CostLine(decimal Quantity, decimal UnitCostUsd, decimal LineCostUsd);

public sealed record IngredientCostLine(
    Guid IngredientId,
    string IngredientName,
    decimal Quantity,
    string UseUnit,
    decimal UnitCostUsd,
    decimal LineCostUsd) : CostLine(Quantity, UnitCostUsd, LineCostUsd);

public sealed record SubRecipeCostLine(
    Guid SubRecipeId,
    string SubRecipeName,
    decimal Quantity,
    string YieldUnit,
    decimal UnitCostUsd,
    decimal LineCostUsd,
    RecipeCostBreakdown SubBreakdown) : CostLine(Quantity, UnitCostUsd, LineCostUsd);
