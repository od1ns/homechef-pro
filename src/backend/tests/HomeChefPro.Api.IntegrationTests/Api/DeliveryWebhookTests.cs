using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Commands.AdvanceOrderStatus;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Application.Payments.Dtos;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Api.IntegrationTests.Api;

[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class DeliveryWebhookTests
{
    private readonly LiveDatabaseFixture _fixture;

    public DeliveryWebhookTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateApi() =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
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
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: $"delivery-admin-{Guid.NewGuid():N}@hcp.test",
            Password: "Test1234",
            FullName: "Delivery Admin",
            Roles: [Roles.Admin]));
        reg.EnsureSuccessStatusCode();
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Webhook_rejects_bad_json()
    {
        await using var factory = CreateApi();
        using var anon = factory.CreateClient();

        var resp = await anon.PostAsync("/api/webhooks/delivery/yummy",
            new StringContent("not-json", System.Text.Encoding.UTF8, "application/json"));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_updates_tracking_and_advances_order_on_delivered()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        // Create a full third-party order all the way to InDelivery.
        var createDish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes", new CreateDishCommand(
            Name: $"Plato delivery {Guid.NewGuid():N}",
            SellingPriceUsd: 6m));
        var dishId = (await createDish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using var anon = factory.CreateClient();
        var orderResp = await anon.PostAsJsonAsync("/api/client/orders", new CreateGuestOrderCommand(
            GuestFullName: "Delivery Client",
            GuestPhone: "+58 412-111-0002",
            DeliveryType: "third_party",
            DeliveryAddress: "Av Principal, Los Palos Grandes",
            Items: [new OrderLineInput(dishId, 1)]));
        var orderId = (await orderResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // Submit + verify payment
        var payResp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(
                OrderId: orderId,
                Method: "pago_movil",
                AmountUsd: 6m,
                PaidCurrency: "VES",
                AmountPaidCurrency: 240m,
                ExchangeRateUsed: 40m));
        var payId = (await payResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);

        // Walk FSM: paid → in_preparation → ready → in_delivery
        foreach (var step in new[] { "in_preparation", "ready", "in_delivery" })
            (await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                new AdminOrdersEndpoints.AdvanceRequest(step))).EnsureSuccessStatusCode();

        // Push events: assigned → on_the_way → delivered
        foreach (var rawStatus in new[] { "pending", "in_transit", "delivered" })
        {
            var payload = new
            {
                orderId,
                status = rawStatus,
                courierName = "Carlos Repartidor",
                courierPhone = "+58 412-333-4444",
                lat = 10.5m,
                lng = -66.9m,
                externalTrackingId = "YUM-42",
            };
            var resp = await anon.PostAsJsonAsync("/api/webhooks/delivery/yummy", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
        }

        // Final order status = delivered.
        var final = await admin.GetFromJsonAsync<OrderDto>($"/api/admin/orders/{orderId}");
        final!.Status.Should().Be("delivered");

        // Tracking + events persisted.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
        var tracking = await db.DeliveryTrackings.FirstAsync(t => t.OrderId == orderId);
        tracking.CurrentStatus.Should().Be(HomeChefPro.Domain.Delivery.DeliveryStatus.Delivered);
        tracking.CourierName.Should().Be("Carlos Repartidor");
        tracking.LastKnownLat.Should().Be(10.5m);

        var events = await db.DeliveryEvents.Where(e => e.OrderId == orderId)
            .OrderBy(e => e.ReceivedAt).ToListAsync();
        events.Should().HaveCount(3);
        events.Select(e => e.NormalizedStatus).Should().ContainInOrder(
            HomeChefPro.Domain.Delivery.DeliveryStatus.Assigned,
            HomeChefPro.Domain.Delivery.DeliveryStatus.OnTheWay,
            HomeChefPro.Domain.Delivery.DeliveryStatus.Delivered);
    }

    private sealed record IdResponse(Guid Id);
}
