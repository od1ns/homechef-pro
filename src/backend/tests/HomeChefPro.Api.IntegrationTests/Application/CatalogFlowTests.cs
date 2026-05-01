using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using HomeChefPro.Application.Catalog.Ingredients.Commands.AddPresentation;
using HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;
using HomeChefPro.Application.Catalog.Ingredients.Queries.GetIngredient;
using HomeChefPro.Application.Catalog.Ingredients.Queries.ListIngredients;
using HomeChefPro.Application.Catalog.Recipes.Commands.AddIngredientComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipeCost;

namespace HomeChefPro.Api.IntegrationTests.Application;

[Trait("Category", "Integration")]
public class CatalogFlowTests : IClassFixture<LiveDatabaseFixture>
{
    private readonly LiveDatabaseFixture _fixture;

    public CatalogFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Create_and_list_ingredient_roundtrip()
    {
        await using var host = new ApplicationTestHost(_fixture);

        var id = await host.Mediator.Send(new CreateIngredientCommand(
            Name: "Cilantro nuevo",
            UseUnit: "g",
            ReorderPointUseUnit: 500,
            MinimumStockUseUnit: 100));

        var dto = await host.Mediator.Send(new GetIngredientQuery(id));
        dto.Name.Should().Be("Cilantro nuevo");
        dto.UseUnit.Should().Be("g");
        dto.IsActive.Should().BeTrue();

        var all = await host.Mediator.Send(new ListIngredientsQuery(Search: "cilantro nuevo"));
        all.Should().ContainSingle(i => i.Id == id);
    }

    [Fact]
    public async Task Add_presentation_and_record_purchase_updates_avg_cost_via_trigger()
    {
        await using var host = new ApplicationTestHost(_fixture);

        var ingredientId = await host.Mediator.Send(new CreateIngredientCommand("Harina demo", "g"));
        var presId = await host.Mediator.Send(new AddPresentationCommand(
            IngredientId: ingredientId,
            Name: "Saco 50kg",
            PurchaseUnit: "kg",
            PurchaseQuantity: 50,
            ConversionToUseUnit: 1000));

        // Insert a purchase via raw SQL (DB trigger updates avg cost + stock).
        await host.Db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ingredient_purchases
                (ingredient_id, presentation_id, quantity_purchased, unit_price_usd, total_cost_usd, recorded_by)
            VALUES ({ingredientId}, {presId}, 2, 45, 90, {Guid.NewGuid()})");

        var dto = await host.Mediator.Send(new GetIngredientQuery(ingredientId));
        dto.CurrentStockUseUnit.Should().Be(100000m);               // 2 * 50 * 1000 = 100,000g
        dto.AvgCostPerUseUnitUsd.Should().BeApproximately(0.0009m, 0.00001m);   // 90 USD / 100,000g
    }

    [Fact]
    public async Task Get_recipe_cost_returns_cascading_breakdown()
    {
        await using var host = new ApplicationTestHost(_fixture);

        // Build: 1 ingredient (priced via purchase) + 1 dish + component.
        var ing = await host.Mediator.Send(new CreateIngredientCommand("Sal demo", "g"));
        var pres = await host.Mediator.Send(new AddPresentationCommand(
            ing, "Kg", "kg", 1, 1000));

        await host.Db.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ingredient_purchases
                (ingredient_id, presentation_id, quantity_purchased, unit_price_usd, total_cost_usd, recorded_by)
            VALUES ({ing}, {pres}, 1, 2, 2, {Guid.NewGuid()})");

        // avg cost = 2 USD / 1000g = 0.002 / g

        var dishId = await host.Mediator.Send(new CreateDishCommand(
            Name: "Plato cost test",
            SellingPriceUsd: 10m,
            PrepTimeMinutes: 5));

        await host.Mediator.Send(new AddIngredientComponentCommand(
            RecipeId: dishId,
            IngredientId: ing,
            Quantity: 50));  // 50g * 0.002 = 0.10

        var cost = await host.Mediator.Send(new GetRecipeCostQuery(dishId));
        cost.TotalCostUsd.Should().BeApproximately(0.10m, 0.0001m);
        cost.Lines.Should().HaveCount(1);
        cost.Lines[0].Kind.Should().Be("ingredient");
    }
}
