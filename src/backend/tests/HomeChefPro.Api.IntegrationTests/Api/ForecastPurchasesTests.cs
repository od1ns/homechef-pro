using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Ingredients.Commands.AddPresentation;
using HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;
using HomeChefPro.Application.Catalog.Recipes.Commands.AddIngredientComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Inventory.Dtos;
using HomeChefPro.Application.Orders.Commands.AdvanceOrderStatus;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Api.IntegrationTests.Api;

[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class ForecastPurchasesTests
{
    private readonly LiveDatabaseFixture _fixture;

    public ForecastPurchasesTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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

    private static async Task<(HttpClient client, AuthResultDto auth)> AdminClient(
        WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: $"forecast-admin-{Guid.NewGuid():N}@hcp.test",
            Password: "Test1234",
            FullName: "Forecast Admin",
            Roles: [Roles.Admin]));
        reg.EnsureSuccessStatusCode();
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Forecast_predicts_ingredient_quantities_and_cost()
    {
        await using var factory = CreateApi();
        var (admin, _) = await AdminClient(factory);

        // 1) Ingredient priced via purchase (avg cost = 45/50000 = 0.0009/g)
        var ingCreate = await admin.PostAsJsonAsync("/api/admin/ingredients", new CreateIngredientCommand(
            Name: $"Harina forecast {Guid.NewGuid():N}",
            UseUnit: "g",
            ReorderPointUseUnit: 5000m));
        var ingId = (await ingCreate.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var presCreate = await admin.PostAsJsonAsync($"/api/admin/ingredients/{ingId}/presentations",
            new AddPresentationCommand(ingId, "Saco 50kg", "kg", 50, 1000));
        var presId = (await presCreate.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ingredient_purchases
                    (ingredient_id, presentation_id, quantity_purchased, unit_price_usd, total_cost_usd, recorded_by)
                VALUES ({ingId}, {presId}, 1, 45, 45, {Guid.NewGuid()})");
        }

        // 2) Dish using 100g per serving.
        var dishResp = await admin.PostAsJsonAsync("/api/admin/recipes/dishes", new CreateDishCommand(
            Name: $"Pan forecast {Guid.NewGuid():N}",
            SellingPriceUsd: 3m));
        var dishId = (await dishResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        (await admin.PostAsJsonAsync($"/api/admin/recipes/{dishId}/components/ingredient",
            new AddIngredientComponentCommand(dishId, ingId, 100))).EnsureSuccessStatusCode();

        // 3) Create 7 delivered orders of 2 units each during the last week.
        //    The seeded SQL does not auto-move orders; we walk them via API.
        for (int i = 0; i < 7; i++)
        {
            using var anon = factory.CreateClient();
            var orderResp = await anon.PostAsJsonAsync("/api/client/orders", new CreateGuestOrderCommand(
                GuestFullName: $"C{i}",
                GuestPhone: "+58 412-000-0000",
                DeliveryType: "pickup",
                Items: [new OrderLineInput(dishId, 2)]));
            var orderId = (await orderResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

            var payResp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
                new SubmitPaymentProofCommand(orderId, "pago_movil", 6m, "VES", 240m, 40m));
            var payId = (await payResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;
            await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);

            foreach (var step in new[] { "in_preparation", "ready", "delivered" })
                (await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                    new AdminOrdersEndpoints.AdvanceRequest(step))).EnsureSuccessStatusCode();
        }

        // 4) Ask for a 7-day forecast based on 28 days of history.
        var forecast = await admin.GetFromJsonAsync<PurchaseForecastDto>(
            "/api/admin/purchasing/forecast?historicalDays=28&targetDays=7&growthFactor=1.0");
        forecast.Should().NotBeNull();
        forecast!.OrdersAnalyzed.Should().BeGreaterThanOrEqualTo(7);

        var line = forecast.Lines.Should().ContainSingle(l => l.IngredientId == ingId).Subject;
        // 7 orders × 2 items × 100g = 1400g consumed in the window.
        line.HistoricalConsumedUseUnit.Should().Be(1400m);
        // Scaled to next 7 of 28 days at growth=1.0 → 350g projected.
        line.ProjectedUseUnit.Should().Be(350m);
        // Stock is 0 (we didn't consume from it — purchase is pure data), so reorder_point 5000
        // dominates the suggestion.
        line.SuggestedPurchaseUseUnit.Should().BeGreaterThanOrEqualTo(5000m);
        line.AvgCostPerUseUnitUsd.Should().BeApproximately(0.0009m, 0.00001m);
        line.EstimatedCostUsd.Should().NotBeNull();
    }

    private sealed record IdResponse(Guid Id);
}
