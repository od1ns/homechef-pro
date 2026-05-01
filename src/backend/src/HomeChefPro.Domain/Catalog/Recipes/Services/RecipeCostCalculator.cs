using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Catalog.Recipes.Services;

/// <summary>
/// Pure-domain cost calculator. Given an in-memory catalog of ingredients and recipes,
/// computes the total cost of a recipe by summing ingredient costs and sub-recipe costs,
/// proportionally scaled by each sub-recipe's yield.
///
/// Formula (per recipe R):
///   total(R) = Σ (component.quantity * unit_cost(component))
///   where unit_cost(ingredient) = ingredient.avg_cost_per_use_unit_usd
///         unit_cost(subRecipe)  = total(subRecipe) / subRecipe.yield_quantity
/// </summary>
public sealed class RecipeCostCalculator
{
    private readonly Dictionary<Guid, Ingredient> _ingredients;
    private readonly Dictionary<Guid, Recipe> _recipes;

    public RecipeCostCalculator(
        IEnumerable<Ingredient> ingredients,
        IEnumerable<Recipe> recipes)
    {
        ArgumentNullException.ThrowIfNull(ingredients);
        ArgumentNullException.ThrowIfNull(recipes);
        _ingredients = ingredients.ToDictionary(i => i.Id);
        _recipes = recipes.ToDictionary(r => r.Id);
    }

    public RecipeCostBreakdown Calculate(Guid recipeId)
    {
        var memo = new Dictionary<Guid, RecipeCostBreakdown>();
        var inStack = new HashSet<Guid>();
        return Resolve(recipeId, memo, inStack);
    }

    /// <summary>
    /// Flattens a recipe into the total use-unit quantity of each raw ingredient required
    /// to produce one batch (one plate for a dish; one yield for a sub-recipe).
    /// Useful for shopping lists and "scale to N portions" scenarios.
    /// </summary>
    public IReadOnlyDictionary<Guid, decimal> FlattenIngredients(Guid recipeId)
    {
        var acc = new Dictionary<Guid, decimal>();
        var inStack = new HashSet<Guid>();
        AccumulateIngredients(recipeId, multiplier: 1m, acc, inStack);
        return acc;
    }

    private RecipeCostBreakdown Resolve(
        Guid recipeId,
        Dictionary<Guid, RecipeCostBreakdown> memo,
        HashSet<Guid> inStack)
    {
        if (memo.TryGetValue(recipeId, out var cached))
            return cached;

        if (!inStack.Add(recipeId))
            throw new DomainException($"Cycle detected in recipe graph at recipe {recipeId}.");

        var recipe = GetRecipe(recipeId);

        var lines = new List<CostLine>(recipe.Components.Count);
        decimal total = 0m;

        foreach (var component in recipe.Components.OrderBy(c => c.DisplayOrder))
        {
            if (component.IsIngredient)
            {
                var ingredient = GetIngredient(component.IngredientId!.Value);
                var unitCost = ingredient.AvgCostPerUseUnitUsd;
                var lineCost = component.Quantity * unitCost;
                total += lineCost;
                lines.Add(new IngredientCostLine(
                    IngredientId: ingredient.Id,
                    IngredientName: ingredient.Name,
                    Quantity: component.Quantity,
                    UseUnit: EnumDbMap<UseUnit>.ToDb(ingredient.UseUnit),
                    UnitCostUsd: unitCost,
                    LineCostUsd: lineCost));
            }
            else
            {
                var subBreakdown = Resolve(component.SubRecipeId!.Value, memo, inStack);
                if (subBreakdown.YieldQuantity is not { } yieldQty || yieldQty <= 0)
                    throw new DomainException(
                        $"Sub-recipe {subBreakdown.RecipeId} has no positive yield quantity.");
                var unitCost = subBreakdown.TotalCostUsd / yieldQty;
                var lineCost = component.Quantity * unitCost;
                total += lineCost;
                lines.Add(new SubRecipeCostLine(
                    SubRecipeId: subBreakdown.RecipeId,
                    SubRecipeName: subBreakdown.RecipeName,
                    Quantity: component.Quantity,
                    YieldUnit: subBreakdown.YieldUnit ?? "",
                    UnitCostUsd: unitCost,
                    LineCostUsd: lineCost,
                    SubBreakdown: subBreakdown));
            }
        }

        inStack.Remove(recipeId);

        var costPerYieldUnit = recipe is { IsSubRecipe: true, YieldQuantity: > 0 }
            ? total / recipe.YieldQuantity.Value
            : (decimal?)null;

        var breakdown = new RecipeCostBreakdown(
            RecipeId: recipe.Id,
            RecipeName: recipe.Name,
            IsSubRecipe: recipe.IsSubRecipe,
            TotalCostUsd: total,
            YieldQuantity: recipe.YieldQuantity,
            YieldUnit: recipe.YieldUnit is null ? null : EnumDbMap<YieldUnit>.ToDb(recipe.YieldUnit.Value),
            CostPerYieldUnit: costPerYieldUnit,
            Lines: lines);

        memo[recipeId] = breakdown;
        return breakdown;
    }

    private void AccumulateIngredients(
        Guid recipeId,
        decimal multiplier,
        Dictionary<Guid, decimal> acc,
        HashSet<Guid> inStack)
    {
        if (!inStack.Add(recipeId))
            throw new DomainException($"Cycle detected in recipe graph at recipe {recipeId}.");

        var recipe = GetRecipe(recipeId);

        foreach (var component in recipe.Components)
        {
            if (component.IsIngredient)
            {
                var id = component.IngredientId!.Value;
                var qty = component.Quantity * multiplier;
                acc[id] = acc.TryGetValue(id, out var prev) ? prev + qty : qty;
            }
            else
            {
                var subRecipe = GetRecipe(component.SubRecipeId!.Value);
                if (subRecipe.YieldQuantity is not { } yieldQty || yieldQty <= 0)
                    throw new DomainException(
                        $"Sub-recipe {subRecipe.Id} has no positive yield quantity.");

                // Using `component.Quantity` of this sub-recipe means producing
                // `component.Quantity / yieldQty` batches of it — scale children accordingly.
                var childMultiplier = multiplier * (component.Quantity / yieldQty);
                AccumulateIngredients(subRecipe.Id, childMultiplier, acc, inStack);
            }
        }

        inStack.Remove(recipeId);
    }

    private Ingredient GetIngredient(Guid id) =>
        _ingredients.TryGetValue(id, out var i)
            ? i
            : throw new DomainException($"Ingredient {id} not found in calculator catalog.");

    private Recipe GetRecipe(Guid id) =>
        _recipes.TryGetValue(id, out var r)
            ? r
            : throw new DomainException($"Recipe {id} not found in calculator catalog.");
}
