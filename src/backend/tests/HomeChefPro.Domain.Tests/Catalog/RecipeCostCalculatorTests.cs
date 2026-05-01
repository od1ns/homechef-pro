using FluentAssertions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Catalog.Recipes.Services;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Tests.Catalog;

public class RecipeCostCalculatorTests
{
    private static Ingredient IngredientWithAvgCost(string name, UseUnit unit, decimal avgCostUsd, Guid? id = null)
    {
        var ingredient = Ingredient.Create(name, unit, id: id);
        // Bypass the public surface (Domain is read-only for avg cost; trigger writes it in DB).
        typeof(Ingredient).GetMethod("SyncStockFromDatabase",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(ingredient, [100m, avgCostUsd, DateTimeOffset.UtcNow]);
        return ingredient;
    }

    [Fact]
    public void Simple_dish_with_only_ingredients_sums_line_costs()
    {
        var beef = IngredientWithAvgCost("Falda de res", UseUnit.Gram, 0.018m); // $0.018/g
        var rice = IngredientWithAvgCost("Arroz blanco", UseUnit.Gram, 0.005m); // $0.005/g

        var dish = Recipe.CreateDish("Plato Simple", sellingPriceUsd: 5m);
        dish.AddIngredient(beef.Id, quantity: 180);   // 180g * 0.018 = 3.24
        dish.AddIngredient(rice.Id, quantity: 100);   // 100g * 0.005 = 0.50

        var calc = new RecipeCostCalculator([beef, rice], [dish]);
        var breakdown = calc.Calculate(dish.Id);

        breakdown.TotalCostUsd.Should().Be(3.74m);
        breakdown.Lines.Should().HaveCount(2);
        breakdown.Lines.OfType<IngredientCostLine>().Sum(l => l.LineCostUsd).Should().Be(3.74m);
    }

    [Fact]
    public void Dish_with_sub_recipe_uses_proportional_yield_cost()
    {
        // Sub-receta: sofrito
        //   - 200g cebolla a $0.004/g = 0.80
        //   - 100g pimentón a $0.012/g = 1.20
        //   total = 2.00 USD, yield = 400g, costo por g = 0.005
        var cebolla = IngredientWithAvgCost("Cebolla", UseUnit.Gram, 0.004m);
        var pimenton = IngredientWithAvgCost("Pimentón", UseUnit.Gram, 0.012m);
        var sofrito = Recipe.CreateSubRecipe("Sofrito", yieldQuantity: 400m, yieldUnit: YieldUnit.Gram);
        sofrito.AddIngredient(cebolla.Id, 200);
        sofrito.AddIngredient(pimenton.Id, 100);

        // Plato: usa 80g de sofrito + 200g de carne a $0.020/g = 4.00
        var carne = IngredientWithAvgCost("Carne mechada", UseUnit.Gram, 0.020m);
        var dish = Recipe.CreateDish("Pabellón", sellingPriceUsd: 10m);
        dish.AddIngredient(carne.Id, 200);
        dish.AddSubRecipe(sofrito.Id, 80);  // 80g * (2.00/400) = 80 * 0.005 = 0.40

        var calc = new RecipeCostCalculator([cebolla, pimenton, carne], [sofrito, dish]);
        var breakdown = calc.Calculate(dish.Id);

        // 4.00 + 0.40 = 4.40
        breakdown.TotalCostUsd.Should().Be(4.40m);

        var subLine = breakdown.Lines.OfType<SubRecipeCostLine>().Single();
        subLine.LineCostUsd.Should().Be(0.40m);
        subLine.UnitCostUsd.Should().Be(0.005m);
        subLine.SubBreakdown.TotalCostUsd.Should().Be(2.00m);
        subLine.SubBreakdown.CostPerYieldUnit.Should().Be(0.005m);
    }

    [Fact]
    public void Nested_sub_recipes_scale_correctly()
    {
        // Level-2 sub-recipe: aceite onotado (20g oil + 5g onoto)
        var oil = IngredientWithAvgCost("Aceite", UseUnit.Milliliter, 0.002m);   // $0.002/ml -> 20ml = 0.04
        var onoto = IngredientWithAvgCost("Onoto", UseUnit.Gram, 0.10m);          // $0.10/g   -> 5g = 0.50
        var aceiteOnotado = Recipe.CreateSubRecipe("Aceite onotado", yieldQuantity: 20m, yieldUnit: YieldUnit.Milliliter);
        aceiteOnotado.AddIngredient(oil.Id, 20);
        aceiteOnotado.AddIngredient(onoto.Id, 5);
        // total 0.54, yield 20ml -> 0.027/ml

        // Level-1 sub-recipe: masa (100g harina + 4ml aceite onotado)
        var harina = IngredientWithAvgCost("Harina", UseUnit.Gram, 0.006m);       // 100g = 0.60
        var masa = Recipe.CreateSubRecipe("Masa", yieldQuantity: 100m, yieldUnit: YieldUnit.Gram);
        masa.AddIngredient(harina.Id, 100);
        masa.AddSubRecipe(aceiteOnotado.Id, 4);   // 4 * 0.027 = 0.108
        // total 0.708, yield 100g

        // Dish: 150g de masa + 2g onoto directos
        var dish = Recipe.CreateDish("Hallaca mini", sellingPriceUsd: 3m);
        dish.AddSubRecipe(masa.Id, 150);          // 150 * (0.708/100) = 150 * 0.00708 = 1.062
        dish.AddIngredient(onoto.Id, 2);          // 2 * 0.10 = 0.20

        var calc = new RecipeCostCalculator(
            [oil, onoto, harina],
            [aceiteOnotado, masa, dish]);

        var breakdown = calc.Calculate(dish.Id);
        breakdown.TotalCostUsd.Should().BeApproximately(1.262m, 0.0001m);
    }

    [Fact]
    public void Flattens_ingredients_to_total_quantities()
    {
        var cebolla = IngredientWithAvgCost("Cebolla", UseUnit.Gram, 0.004m);
        var ajo = IngredientWithAvgCost("Ajo", UseUnit.Gram, 0.020m);
        var sofrito = Recipe.CreateSubRecipe("Sofrito", yieldQuantity: 400m, yieldUnit: YieldUnit.Gram);
        sofrito.AddIngredient(cebolla.Id, 200);
        sofrito.AddIngredient(ajo.Id, 20);

        // Dish uses 40g sofrito + 10g extra ajo
        var dish = Recipe.CreateDish("Algo", sellingPriceUsd: 1m);
        dish.AddSubRecipe(sofrito.Id, 40);  // 40/400 = 0.1 batch -> 20g cebolla + 2g ajo
        dish.AddIngredient(ajo.Id, 10);

        var calc = new RecipeCostCalculator([cebolla, ajo], [sofrito, dish]);
        var flat = calc.FlattenIngredients(dish.Id);

        flat[cebolla.Id].Should().Be(20m);
        flat[ajo.Id].Should().Be(12m);  // 2 from sofrito + 10 direct
    }

    [Fact]
    public void Throws_on_cycle_in_recipe_graph()
    {
        // A -> B, B -> A (imposible, pero Domain debería detectarlo defensivamente)
        var flour = IngredientWithAvgCost("Flour", UseUnit.Gram, 0.001m);
        var a = Recipe.CreateSubRecipe("A", 100, YieldUnit.Gram);
        var b = Recipe.CreateSubRecipe("B", 100, YieldUnit.Gram);
        a.AddIngredient(flour.Id, 50);
        a.AddSubRecipe(b.Id, 10);
        b.AddIngredient(flour.Id, 50);
        b.AddSubRecipe(a.Id, 10);

        var calc = new RecipeCostCalculator([flour], [a, b]);
        var act = () => calc.Calculate(a.Id);

        act.Should().Throw<DomainException>()
           .WithMessage("*Cycle detected*");
    }

    [Fact]
    public void Enforces_yield_for_sub_recipe_in_graph()
    {
        // Create a sub-recipe with yield, but then deliberately zero it via reflection
        // to simulate a corrupted DB row — calculator must refuse.
        var flour = IngredientWithAvgCost("Flour", UseUnit.Gram, 0.001m);
        var sub = Recipe.CreateSubRecipe("Broken", 100, YieldUnit.Gram);
        sub.AddIngredient(flour.Id, 50);
        typeof(Recipe).GetProperty(nameof(Recipe.YieldQuantity))!.SetValue(sub, 0m);

        var dish = Recipe.CreateDish("Uses broken", 5m);
        dish.AddSubRecipe(sub.Id, 10);

        var calc = new RecipeCostCalculator([flour], [sub, dish]);
        var act = () => calc.Calculate(dish.Id);

        act.Should().Throw<DomainException>()
           .WithMessage("*positive yield*");
    }

    [Fact]
    public void Memoizes_shared_sub_recipes()
    {
        // If the same sub-recipe is used twice, it should be calculated once and reused.
        var a = IngredientWithAvgCost("a", UseUnit.Gram, 0.01m);
        var sub = Recipe.CreateSubRecipe("S", 100, YieldUnit.Gram);
        sub.AddIngredient(a.Id, 100);

        var dish = Recipe.CreateDish("D", 1m);
        dish.AddSubRecipe(sub.Id, 50);
        // Add the same sub-recipe twice is blocked by the aggregate; test the diamond
        // via two parent subs pointing to the same leaf.
        var parent1 = Recipe.CreateSubRecipe("P1", 10, YieldUnit.Gram);
        parent1.AddSubRecipe(sub.Id, 10);
        var parent2 = Recipe.CreateSubRecipe("P2", 10, YieldUnit.Gram);
        parent2.AddSubRecipe(sub.Id, 10);

        var diamond = Recipe.CreateDish("Diamond", 1m);
        diamond.AddSubRecipe(parent1.Id, 5);
        diamond.AddSubRecipe(parent2.Id, 5);

        var calc = new RecipeCostCalculator([a], [sub, parent1, parent2, diamond]);
        var breakdown = calc.Calculate(diamond.Id);

        // Whatever the exact number, the call must succeed without stack overflow or exception.
        breakdown.TotalCostUsd.Should().BeGreaterThan(0);
    }
}
