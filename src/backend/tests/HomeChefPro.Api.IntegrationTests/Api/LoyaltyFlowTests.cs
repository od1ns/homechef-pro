using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Loyalty.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Api.IntegrationTests.Api;

/// <summary>
/// Tests integration del modulo Sabor (loyalty):
///   GET  /api/client/loyalty/me       -> balance, nivel, puntos al siguiente.
///   GET  /api/client/loyalty/rewards  -> catalogo de recompensas activas.
/// La acreditacion de puntos al delivered y el redeem se cubren en
/// pruebas manuales (smoke-deep / seed-rich-history) porque requieren un
/// flow de orden completo + entrega.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class LoyaltyFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public LoyaltyFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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
                    ["Bootstrap:RequireInvitationCode"] = "false",
                    ["RateLimiting:Disabled"]      = "true",
            }));
        });

    private static async Task<HttpClient> RegisterClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: $"loyalty-{Guid.NewGuid():N}@hcp.test",
            Password: "Test1234",
            FullName: "Cliente Loyalty"));
        reg.EnsureSuccessStatusCode();
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResultDto>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task New_client_starts_with_zero_balance_and_bronze_level()
    {
        await using var factory = CreateApi();
        var client = await RegisterClient(factory);

        var account = await client.GetFromJsonAsync<LoyaltyAccountDto>("/api/client/loyalty/me");

        account.Should().NotBeNull();
        account!.CurrentBalance.Should().Be(0);
        account.LifetimeEarned.Should().Be(0);
        account.Level.Should().Be("bronce");
        account.NextLevel.Should().Be("plata");
        // Para alcanzar plata se necesitan 500 puntos.
        account.PointsToNextLevel.Should().Be(500);
    }

    [Fact]
    public async Task Rewards_catalog_returns_active_items_with_affordability_flag()
    {
        await using var factory = CreateApi();
        var client = await RegisterClient(factory);

        var rewards = await client.GetFromJsonAsync<LoyaltyRewardDto[]>("/api/client/loyalty/rewards");

        rewards.Should().NotBeNull();
        // El seed inicial debe sembrar al menos 1 recompensa (postre, descuento, etc).
        rewards!.Should().NotBeEmpty();
        // Cliente recien registrado tiene balance=0; ninguna recompensa con cost>0
        // deberia ser accesible.
        rewards!.Should().OnlyContain(r => r.CostPoints > 0);
        rewards!.Should().OnlyContain(r => !r.IsAffordable); // todas inalcanzables
        rewards!.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Name));
        rewards!.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.RewardType));
    }

    [Fact]
    public async Task Redeem_with_zero_balance_returns_conflict()
    {
        await using var factory = CreateApi();
        var client = await RegisterClient(factory);

        // Buscar primera recompensa activa.
        var rewards = await client.GetFromJsonAsync<LoyaltyRewardDto[]>("/api/client/loyalty/rewards");
        rewards.Should().NotBeNull().And.NotBeEmpty();
        var firstRewardId = rewards![0].Id;

        var redeem = await client.PostAsync($"/api/client/loyalty/redeem/{firstRewardId}", null);

        // El cliente tiene 0 puntos -> debe rechazar con 409 (DomainException).
        redeem.StatusCode.Should().Be(System.Net.HttpStatusCode.Conflict);
    }
}
