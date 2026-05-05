using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.LoginUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Ingredients.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Catalog.Recipes.Dtos;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Orders.Dtos;
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
public class EndToEndFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public EndToEndFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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
                ["Jwt:AccessTokenMinutes"]      = "60",
                ["Bootstrap:EnableOnStart"]     = "false",
                    ["RateLimiting:Disabled"]      = "true",
            }));
        });

    private static async Task<HttpClient> AuthenticatedClient(
        WebApplicationFactory<Program> factory, params string[] roles)
    {
        var client = factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@hcp.test";
        var auth = await IdentityTestHelpers.RegisterAndAssignRolesAsync(
            factory, client, email, "Test1234", "E2E User", roles.Length > 0 ? roles : [Roles.Admin]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Client_without_token_can_browse_menu_but_cannot_see_admin()
    {
        await using var factory = CreateApi();
        using var anon = factory.CreateClient();

        (await anon.GetAsync("/api/client/menu")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await anon.GetAsync("/api/admin/ingredients")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Full_journey_admin_creates_dish_client_orders_admin_verifies_kitchen_ready()
    {
        await using var factory = CreateApi();
        using var admin = await AuthenticatedClient(factory, Roles.Admin);

        // 1. Create ingredient + presentation
        var createIng = await admin.PostAsJsonAsync("/api/admin/ingredients", new
        {
            name = $"Harina E2E {Guid.NewGuid():N}",
            useUnit = "g",
            reorderPointUseUnit = 500m,
        });
        createIng.EnsureSuccessStatusCode();
        var ingPayload = await createIng.Content.ReadFromJsonAsync<IdResponse>();
        var ingredientId = ingPayload!.Id;

        var createPres = await admin.PostAsJsonAsync(
            $"/api/admin/ingredients/{ingredientId}/presentations",
            new AdminIngredientsEndpoints.AddPresentationRequest(
                Name: "Saco 50kg",
                PurchaseUnit: "kg",
                PurchaseQuantity: 50,
                ConversionToUseUnit: 1000));
        createPres.EnsureSuccessStatusCode();
        var presentationId = (await createPres.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // 2. Inject a purchase so the trigger updates avg_cost_per_use_unit_usd.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($@"
                INSERT INTO ingredient_purchases
                    (ingredient_id, presentation_id, quantity_purchased, unit_price_usd, total_cost_usd, recorded_by)
                VALUES ({ingredientId}, {presentationId}, 1, 45, 45, {Guid.NewGuid()})");
        }

        // 3. Create a dish + component
        var createDish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes", new CreateDishCommand(
            Name: $"Pan E2E {Guid.NewGuid():N}",
            SellingPriceUsd: 3m,
            PrepTimeMinutes: 5));
        createDish.EnsureSuccessStatusCode();
        var dishId = (await createDish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var addComp = await admin.PostAsJsonAsync(
            $"/api/admin/recipes/{dishId}/components/ingredient",
            new AdminRecipesEndpoints.AddIngredientComponentRequest(
                IngredientId: ingredientId,
                Quantity: 100));  // 100g
        addComp.EnsureSuccessStatusCode();

        // 4. Query cost endpoint
        var costResp = await admin.GetAsync($"/api/admin/recipes/{dishId}/cost");
        costResp.EnsureSuccessStatusCode();
        var cost = await costResp.Content.ReadFromJsonAsync<RecipeCostDto>();
        cost!.TotalCostUsd.Should().BeApproximately(0.09m, 0.001m);   // 100g * (45/50000) = 0.09

        // 5. Client (anon) browses menu and creates guest order
        using var anon = factory.CreateClient();
        var menuResp = await anon.GetAsync("/api/client/menu");
        menuResp.EnsureSuccessStatusCode();
        var menu = await menuResp.Content.ReadFromJsonAsync<RecipeSummaryDto[]>();
        menu.Should().NotBeNull();
        menu!.Should().ContainSingle(r => r.Id == dishId);

        var orderResp = await anon.PostAsJsonAsync("/api/client/orders", new CreateGuestOrderCommand(
            GuestFullName: "Cliente E2E",
            GuestPhone: "+58 412-555-9000",
            DeliveryType: "pickup",
            Items: [new OrderLineInput(dishId, 2)]));
        orderResp.EnsureSuccessStatusCode();
        var orderId = (await orderResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // 6. Client submits payment proof
        var paymentResp = await anon.PostAsJsonAsync(
            $"/api/client/orders/{orderId}/payment",
            new ClientOrdersEndpoints.SubmitPaymentRequest(
                Method: "pago_movil",
                AmountUsd: 6m,
                PaidCurrency: "VES",
                AmountPaidCurrency: 240m,
                ExchangeRateUsed: 40m,
                ReferenceNumber: "TEST-001"));
        paymentResp.EnsureSuccessStatusCode();
        var paymentId = (await paymentResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // Order is now payment_verifying
        var afterSubmit = await admin.GetFromJsonAsync<OrderDto>($"/api/admin/orders/{orderId}");
        afterSubmit!.Status.Should().Be("payment_verifying");

        // 7. Admin verifies payment
        (await admin.PostAsync($"/api/admin/payments/{paymentId}/verify", null))
            .EnsureSuccessStatusCode();

        var afterPaid = await admin.GetFromJsonAsync<OrderDto>($"/api/admin/orders/{orderId}");
        afterPaid!.Status.Should().Be("paid");

        // 8. Cook starts and finishes the single item
        using var cook = await AuthenticatedClient(factory, Roles.Cook);
        var itemId = afterPaid.Items[0].Id;

        (await cook.PostAsync($"/api/kitchen/orders/{orderId}/items/{itemId}/start", null))
            .EnsureSuccessStatusCode();
        (await cook.PostAsync($"/api/kitchen/orders/{orderId}/items/{itemId}/ready", null))
            .EnsureSuccessStatusCode();

        // Single item + ready → order status auto-advances to 'ready'.
        var afterReady = await admin.GetFromJsonAsync<OrderDto>($"/api/admin/orders/{orderId}");
        afterReady!.Status.Should().Be("ready");

        // 9. Admin marks delivered
        (await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
            new AdminOrdersEndpoints.AdvanceRequest("delivered"))).EnsureSuccessStatusCode();

        var done = await admin.GetFromJsonAsync<OrderDto>($"/api/admin/orders/{orderId}");
        done!.Status.Should().Be("delivered");
        done.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Validation_failures_return_400_with_problem_details()
    {
        await using var factory = CreateApi();
        using var admin = await AuthenticatedClient(factory, Roles.Admin);

        var badCreate = await admin.PostAsJsonAsync("/api/admin/ingredients", new
        {
            name = "",            // blank
            useUnit = "invalid",  // not in enum
        });
        badCreate.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Client_role_cannot_hit_admin_endpoints()
    {
        await using var factory = CreateApi();
        using var client = await AuthenticatedClient(factory, Roles.Client);

        (await client.GetAsync("/api/admin/ingredients")).StatusCode
            .Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record IdResponse(Guid Id);
}
