using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Ingredients.Commands.AddPresentation;
using HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;
using HomeChefPro.Application.Catalog.Recipes.Commands.AddIngredientComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Application.Reports.Dtos;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HomeChefPro.Api.IntegrationTests.Helpers;

namespace HomeChefPro.Api.IntegrationTests.Api;

[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class ReportsTests
{
    private readonly LiveDatabaseFixture _fixture;
    public ReportsTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateApi() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestDatabase(_fixture.ConnectionString);
            b.UseTestAuth();
            b.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = _fixture.ConnectionString,
                ["ConnectionStrings:Redis"]     = "",
                ["Jwt:Issuer"]                  = "HomeChefPro-Test",
                ["Jwt:Audience"]                = "HomeChefPro-Clients-Test",
                ["Jwt:SigningKey"]              = new string('x', 64),
                ["Bootstrap:EnableOnStart"]     = "false",
            }));
        });

    private static async Task<HttpClient> AdminClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var auth = await IdentityTestHelpers.RegisterAndAssignRolesAsync(
            factory, client, $"reports-{Guid.NewGuid():N}@hcp.test", "Test1234", "Reports Admin", [Roles.Admin]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Dish_margin_and_recipe_costs_reflect_actual_cost()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        // Ingredient @ 0.0009 USD/g (45 USD per 50kg sack)
        var ingId = (await (await admin.PostAsJsonAsync("/api/admin/ingredients", new CreateIngredientCommand(
            Name: $"Harina rep {Guid.NewGuid():N}", UseUnit: "g")))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var presId = (await (await admin.PostAsJsonAsync($"/api/admin/ingredients/{ingId}/presentations",
            new AddPresentationCommand(ingId, "Saco 50kg", "kg", 50, 1000)))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ingredient_purchases
                    (ingredient_id, presentation_id, quantity_purchased, unit_price_usd, total_cost_usd, recorded_by)
                VALUES ({ingId}, {presId}, 1, 45, 45, {Guid.NewGuid()})");
        }

        // Dish @ 5 USD using 100g → cost ~ 0.09 USD, margin ~ 98%.
        var dishId = (await (await admin.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand($"Dish margen {Guid.NewGuid():N}", 5m)))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await admin.PostAsJsonAsync($"/api/admin/recipes/{dishId}/components/ingredient",
            new AddIngredientComponentCommand(dishId, ingId, 100));

        // Recipe full cost report includes our dish.
        var recipeCosts = await admin.GetFromJsonAsync<RecipeFullCostRow[]>(
            "/api/admin/reports/recipe-costs");
        var dishCost = recipeCosts!.Single(r => r.RecipeId == dishId);
        dishCost.TotalCostUsd.Should().BeApproximately(0.09m, 0.001m);

        var margins = await admin.GetFromJsonAsync<DishProfitMarginRow[]>(
            "/api/admin/reports/dish-margin");
        var dishMargin = margins!.Single(m => m.DishId == dishId);
        dishMargin.GrossProfitUsd.Should().BeApproximately(4.91m, 0.01m);
        dishMargin.GrossMarginPct.Should().BeGreaterThan(95m);
    }

    [Fact]
    public async Task Reorder_suggestions_flag_critical_when_below_minimum()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        // Ingredient with reorder_point 100 and minimum_stock 50, but 0 stock → critical.
        var ingId = (await (await admin.PostAsJsonAsync("/api/admin/ingredients", new CreateIngredientCommand(
            Name: $"Sal critica {Guid.NewGuid():N}",
            UseUnit: "g",
            ReorderPointUseUnit: 100m,
            MinimumStockUseUnit: 50m)))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var rows = await admin.GetFromJsonAsync<ReorderSuggestionRow[]>(
            "/api/admin/reports/reorder-suggestions?priority=critical");
        rows!.Should().Contain(r => r.IngredientId == ingId && r.Priority == "critical");
    }

    [Fact]
    public async Task Sales_daily_returns_window_data_after_delivered_orders()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        var dishId = (await (await admin.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand($"Dish ventas {Guid.NewGuid():N}", 4m)))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // 2 delivered orders today
        for (int i = 0; i < 2; i++)
        {
            using var anon = factory.CreateClient();
            var orderId = (await (await anon.PostAsJsonAsync("/api/client/orders",
                new CreateGuestOrderCommand($"V{i}", "+58 412-000-0001", "pickup",
                    [new OrderLineInput(dishId, 1)])))
                .Content.ReadFromJsonAsync<IdResponse>())!.Id;
            var payId = (await (await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
                new SubmitPaymentProofCommand(orderId, "pago_movil", 4m, "VES", 160m, 40m)))
                .Content.ReadFromJsonAsync<IdResponse>())!.Id;
            await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);
            foreach (var step in new[] { "in_preparation", "ready", "delivered" })
                await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                    new AdminOrdersEndpoints.AdvanceRequest(step));
        }

        var daily = await admin.GetFromJsonAsync<SalesDailyRow[]>("/api/admin/reports/sales-daily?days=7");
        daily.Should().NotBeNull();
        daily!.Should().NotBeEmpty();
        var today = daily!.OrderByDescending(d => d.SaleDate).First();
        today.OrdersCount.Should().BeGreaterThanOrEqualTo(2);
        today.RevenueUsd.Should().BeGreaterThanOrEqualTo(8m);
    }

    private sealed record IdResponse(Guid Id);
}
