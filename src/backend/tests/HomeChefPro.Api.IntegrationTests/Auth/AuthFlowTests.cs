using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Auth.Commands.LoginUser;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using HomeChefPro.Infrastructure.Persistence;

namespace HomeChefPro.Api.IntegrationTests.Auth;

[Trait("Category", "Integration")]
public class AuthFlowTests : IClassFixture<LiveDatabaseFixture>
{
    private readonly LiveDatabaseFixture _fixture;

    public AuthFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateApi()
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
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
                });
            });
        });
    }

    [Fact]
    public async Task Register_creates_user_and_returns_valid_jwt()
    {
        await using var factory = CreateApi();
        using var client = factory.CreateClient();

        var email = $"chef-{Guid.NewGuid():N}@hcp.test";
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: email,
            Password: "Test1234",
            FullName: "Luisa Probar",
            Phone: "+58 412-000-0001"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AuthResultDto>();
        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be(email);
        result.Roles.Should().Contain("Client");

        // UserProfile persisted.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
        var profile = await db.UserProfiles.FirstAsync(p => p.Id == result.UserId);
        profile.FullName.Should().Be("Luisa Probar");
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        await using var factory = CreateApi();
        using var client = factory.CreateClient();

        var email = $"chef-{Guid.NewGuid():N}@hcp.test";
        (await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            email, "Test1234", "Tester"))).EnsureSuccessStatusCode();

        var bad = await client.PostAsJsonAsync("/api/auth/login", new LoginUserCommand(email, "WRONG-pass-1"));
        bad.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Protected_endpoint_rejects_missing_token_then_accepts_valid_one()
    {
        await using var factory = CreateApi();
        using var client = factory.CreateClient();

        // No token → 401
        var noToken = await client.GetAsync("/api/auth/me");
        noToken.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Register and login → use token
        var email = $"chef-{Guid.NewGuid():N}@hcp.test";
        await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            email, "Test1234", "Yo soy Client", PreferredLanguage: "es-VE"));

        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new LoginUserCommand(email, "Test1234"));
        loginResp.EnsureSuccessStatusCode();
        var auth = (await loginResp.Content.ReadFromJsonAsync<AuthResultDto>())!;

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var meResp = await client.SendAsync(req);
        meResp.EnsureSuccessStatusCode();

        var me = await meResp.Content.ReadFromJsonAsync<UserSummaryDto>();
        me.Should().NotBeNull();
        me!.FullName.Should().Be("Yo soy Client");
        me.Roles.Should().Contain("Client");
    }
}
