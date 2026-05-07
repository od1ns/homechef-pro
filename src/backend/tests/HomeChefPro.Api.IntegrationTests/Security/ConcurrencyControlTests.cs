using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Helpers;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Tenancy;
using HomeChefPro.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Api.IntegrationTests.Security;

/// <summary>
/// F-26 (Tier 2 hardening): optimistic concurrency con xmin de Postgres.
/// Cuando dos contextos modifican la misma row simultaneamente, el segundo
/// SaveChanges debe lanzar DbUpdateConcurrencyException; el endpoint debe
/// mappear a 409.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class ConcurrencyControlTests
{
    private readonly LiveDatabaseFixture _fixture;

    public ConcurrencyControlTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateApi()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestDatabase(_fixture.ConnectionString);
            b.UseTestAuth();
            b.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _fixture.ConnectionString,
                    ["ConnectionStrings:Redis"]     = "",
                    ["Jwt:Issuer"]                  = "HomeChefPro-Test",
                    ["Jwt:Audience"]                = "HomeChefPro-Clients-Test",
                    ["Jwt:SigningKey"]              = new string('x', 64),
                    ["Jwt:AccessTokenMinutes"]      = "60",
                    ["Bootstrap:EnableOnStart"]     = "false",
                    ["Bootstrap:RequireInvitationCode"] = "false",
                    ["RateLimiting:Disabled"]       = "true",
                });
            });
        });
    }

    [Fact]
    public async Task Concurrent_order_update_should_throw_DbUpdateConcurrencyException()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Setup: registrar admin + crear plato + orden anonima.
        var admin = await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"admin-conc-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Admin Concurrency",
            roles: [Roles.Admin]);

        Guid dishId;
        Guid orderId;
        using (var scope = api.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            dishId = await mediator.Send(new CreateDishCommand(
                Name: $"Test Plato {Guid.NewGuid():N}",
                SellingPriceUsd: 5m,
                MenuType: "fixed"));

            var orderResult = await mediator.Send(new CreateGuestOrderCommand(
                GuestFullName: "Concurrency Tester",
                GuestPhone: "+58 412-555-9001",
                DeliveryType: "pickup",
                Items: [new OrderLineInput(dishId, 1)]));
            orderId = orderResult.Id;
        }

        // Concurrencia: dos DbContext separados cargan el mismo Order y cada uno
        // llama SubmitPayment + SaveChanges. El primero gana, el segundo debe tirar
        // DbUpdateConcurrencyException porque el xmin cambio.
        using var scopeA = api.Services.CreateScope();
        using var scopeB = api.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<HomeChefProDbContext>();

        var orderA = await dbA.Orders.SingleAsync(o => o.Id == orderId);
        var orderB = await dbB.Orders.SingleAsync(o => o.Id == orderId);

        // ContextA modifica primero y guarda.
        orderA.SubmitPayment();
        await dbA.SaveChangesAsync();

        // ContextB intenta modificar y guardar — debe fallar.
        orderB.SubmitPayment();
        var act = async () => await dbB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            because: "F-26: el xmin de la row cambio entre el SELECT de B y su UPDATE; EF detecta la colision y aborta.");
    }
}
