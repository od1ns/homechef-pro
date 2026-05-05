using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Application.Reviews.Commands.LeaveReview;
using HomeChefPro.Application.Reviews.Dtos;
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
public class ReviewsFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public ReviewsFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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
                    ["RateLimiting:Disabled"]      = "true",
            }));
        });

    private static async Task<(HttpClient client, AuthResultDto auth)> Register(
        WebApplicationFactory<Program> factory, string[] roles, string? fullName = null)
    {
        var client = factory.CreateClient();
        var auth = await IdentityTestHelpers.RegisterAndAssignRolesAsync(
            factory, client, $"rev-{Guid.NewGuid():N}@hcp.test", "Test1234", fullName ?? "Cliente Reseña", roles);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (client, auth);
    }

    [Fact]
    public async Task Registered_user_can_leave_review_for_delivered_order()
    {
        await using var factory = CreateApi();
        var (admin, _) = await Register(factory, [Roles.Admin], "Admin Reviews");
        var (clientHttp, clientAuth) = await Register(factory, [Roles.Client], "María Fernández");

        // Admin creates a dish.
        var dishCreate = await admin.PostAsJsonAsync("/api/admin/recipes/dishes", new CreateDishCommand(
            Name: $"Arepa reseña {Guid.NewGuid():N}",
            SellingPriceUsd: 5m));
        var dishId = (await dishCreate.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // Create an order owned by the registered client — we don't have a /api/client/orders
        // endpoint for registered flow yet, so seed directly through the DbContext.
        Guid orderId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
            var order = HomeChefPro.Domain.Orders.Order.CreateForRegisteredUser(
                userId: clientAuth.UserId,
                deliveryType: HomeChefPro.Domain.Orders.DeliveryType.Pickup);
            order.AddItem(dishId, "Arepa Reseña", 5m, 2);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        // Client submits payment.
        var payResp = await clientHttp.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(orderId, "pago_movil", 10m, "VES", 400m, 40m));
        payResp.EnsureSuccessStatusCode();

        // Admin verifies and walks to delivered.
        var pending = await admin.GetFromJsonAsync<PaymentInfo[]>("/api/admin/payments/pending");
        var mine = pending!.First(p => p.OrderId == orderId);
        await admin.PostAsync($"/api/admin/payments/{mine.Id}/verify", null);
        foreach (var step in new[] { "in_preparation", "ready", "delivered" })
            await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                new AdminOrdersEndpoints.AdvanceRequest(step));

        // Guest can't leave a review (no token).
        using var anon = factory.CreateClient();
        var anonTry = await anon.PostAsJsonAsync("/api/client/reviews",
            new LeaveReviewCommand(orderId, dishId, 5, "Guest"));
        anonTry.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Registered owner leaves a review.
        var reviewResp = await clientHttp.PostAsJsonAsync("/api/client/reviews",
            new LeaveReviewCommand(orderId, dishId, 5, "Me encantó, sabor casero de verdad."));
        reviewResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Duplicate submission is blocked.
        var dupe = await clientHttp.PostAsJsonAsync("/api/client/reviews",
            new LeaveReviewCommand(orderId, dishId, 4, "Second try"));
        dupe.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Public endpoint returns visible reviews.
        var publicReviews = await anon.GetFromJsonAsync<PublicReviewDto[]>(
            $"/api/client/menu/{dishId}/reviews");
        publicReviews.Should().NotBeNull();
        publicReviews!.Should().ContainSingle(r =>
            r.Rating == 5 && r.CustomerDisplay == "María F.");

        // Admin hides the review.
        var reviewId = publicReviews!.Single().Id;
        (await admin.PostAsJsonAsync($"/api/admin/reviews/{reviewId}/hide",
            new AdminReviewsEndpoints.ModerationNote("Contenido inapropiado"))).EnsureSuccessStatusCode();

        var afterHide = await anon.GetFromJsonAsync<PublicReviewDto[]>(
            $"/api/client/menu/{dishId}/reviews");
        afterHide!.Should().BeEmpty();

        // My reviews still includes it (private view).
        var mineList = await clientHttp.GetFromJsonAsync<ReviewDto[]>("/api/client/reviews/mine");
        mineList!.Should().ContainSingle(r => r.Id == reviewId && !r.IsVisible);
    }

    [Fact]
    public async Task Cannot_review_order_not_delivered()
    {
        await using var factory = CreateApi();
        var (admin, _) = await Register(factory, [Roles.Admin]);
        var (client, clientAuth) = await Register(factory, [Roles.Client]);

        var dishResp = await admin.PostAsJsonAsync("/api/admin/recipes/dishes", new CreateDishCommand(
            Name: $"Sin reseña {Guid.NewGuid():N}", SellingPriceUsd: 4m));
        var dishId = (await dishResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        Guid orderId;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
            var order = HomeChefPro.Domain.Orders.Order.CreateForRegisteredUser(
                userId: clientAuth.UserId,
                deliveryType: HomeChefPro.Domain.Orders.DeliveryType.Pickup);
            order.AddItem(dishId, "Plato", 4m, 1);
            db.Orders.Add(order);
            await db.SaveChangesAsync();
            orderId = order.Id;
        }

        // Order is still pending_payment — review must be rejected.
        var resp = await client.PostAsJsonAsync("/api/client/reviews",
            new LeaveReviewCommand(orderId, dishId, 5, "Too early"));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private sealed record IdResponse(Guid Id);
    private sealed record PaymentInfo(Guid Id, Guid OrderId);
}
