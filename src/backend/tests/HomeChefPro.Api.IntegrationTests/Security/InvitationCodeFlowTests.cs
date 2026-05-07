using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Helpers;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Invitations.Commands.CreateInvitation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace HomeChefPro.Api.IntegrationTests.Security;

/// <summary>
/// Sesion A / Frente 1: invitation codes.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class InvitationCodeFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public InvitationCodeFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    /// <summary>
    /// Crea un WebApplicationFactory CON RequireInvitationCode=true (override
    /// del default false que tienen el resto de tests).
    /// </summary>
    private WebApplicationFactory<Program> CreateApi(bool requireCode = true)
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
                    ["Bootstrap:RequireInvitationCode"] = requireCode ? "true" : "false",
                    ["RateLimiting:Disabled"]       = "true",
                });
            });
        });
    }

    [Fact]
    public async Task Register_should_fail_without_code_when_required()
    {
        await using var api = CreateApi(requireCode: true);
        using var client = api.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(
                Email: $"no-code-{Guid.NewGuid():N}@hcp.test",
                Password: "Test1234",
                FullName: "Sin Codigo"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "Sesion A: registro requiere invitationCode");
    }

    [Fact]
    public async Task Register_should_fail_with_invalid_code()
    {
        await using var api = CreateApi(requireCode: true);
        using var client = api.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(
                Email: $"bad-code-{Guid.NewGuid():N}@hcp.test",
                Password: "Test1234",
                FullName: "Codigo Invalido",
                InvitationCode: "DOESNOTEXIST123"));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_should_succeed_with_valid_code_and_consume_it()
    {
        await using var api = CreateApi(requireCode: true);

        // Setup: admin crea un codigo via mediator directo (mas simple que via HTTP).
        Guid adminUserId;
        string code;
        Guid invitationId;
        using (var setupClient = api.CreateClient())
        {
            // Creamos admin via Helper (necesita registrarse — temporalmente apagamos require).
            // Mejor: creamos admin con un cliente en otro factory.
        }

        // Approach mas robusto: factory con requireCode=false para admin setup,
        // y factory con requireCode=true para el test del cliente.
        using var setupApi = CreateApi(requireCode: false);
        using var adminClient = setupApi.CreateClient();
        var admin = await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            setupApi, adminClient,
            email: $"admin-inv-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Admin Inv",
            roles: [Roles.Admin]);

        // Admin genera un codigo.
        var createResp = await adminClient.PostAsJsonAsync("/api/admin/invitations", new
        {
            maxUses = 1,
            notes = "Test invitation"
        });
        createResp.EnsureSuccessStatusCode();
        var inv = await createResp.Content.ReadFromJsonAsync<InvitationCodeDto>();
        inv.Should().NotBeNull();
        code = inv!.Code;
        invitationId = inv.Id;

        // Cliente se registra con el codigo en el factory original (requireCode=true).
        using var clientApi = CreateApi(requireCode: true);
        using var newClient = clientApi.CreateClient();
        var resp = await newClient.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(
                Email: $"with-code-{Guid.NewGuid():N}@hcp.test",
                Password: "Test1234",
                FullName: "Con Codigo",
                InvitationCode: code));

        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        // Verificar que used_count se incremento (usando admin de antes).
        var listResp = await adminClient.GetAsync("/api/admin/invitations?onlyActive=false");
        listResp.EnsureSuccessStatusCode();
        var list = await listResp.Content.ReadFromJsonAsync<List<InvitationCodeDto>>();
        var updated = list!.First(i => i.Id == invitationId);
        updated.UsedCount.Should().Be(1);
        updated.IsActive.Should().BeFalse(because: "max_uses=1, ya consumido");

        // Otro cliente intenta usar el mismo codigo -> 400.
        using var clientApi2 = CreateApi(requireCode: true);
        using var c2 = clientApi2.CreateClient();
        var second = await c2.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(
                Email: $"second-code-{Guid.NewGuid():N}@hcp.test",
                Password: "Test1234",
                FullName: "Segundo",
                InvitationCode: code));
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "max_uses=1, codigo ya consumido");
    }

    [Fact]
    public async Task Admin_can_revoke_code_then_register_fails()
    {
        using var setupApi = CreateApi(requireCode: false);
        using var adminClient = setupApi.CreateClient();
        var admin = await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            setupApi, adminClient,
            email: $"admin-rev-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Admin Revoke",
            roles: [Roles.Admin]);

        // Crear codigo
        var createResp = await adminClient.PostAsJsonAsync("/api/admin/invitations",
            new { maxUses = 5 });
        createResp.EnsureSuccessStatusCode();
        var inv = await createResp.Content.ReadFromJsonAsync<InvitationCodeDto>();

        // Revocar
        var revokeResp = await adminClient.PostAsJsonAsync(
            $"/api/admin/invitations/{inv!.Id}/revoke",
            new { reason = "Test revoke" });
        revokeResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Cliente intenta registrar con codigo revocado -> 400
        using var clientApi = CreateApi(requireCode: true);
        using var c = clientApi.CreateClient();
        var resp = await c.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(
                Email: $"after-revoke-{Guid.NewGuid():N}@hcp.test",
                Password: "Test1234",
                FullName: "Despues Revoke",
                InvitationCode: inv.Code));
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Admin_endpoints_should_require_admin_role()
    {
        using var api = CreateApi(requireCode: false);
        using var client = api.CreateClient();

        // Sin auth
        var anon = await client.PostAsJsonAsync("/api/admin/invitations", new { maxUses = 1 });
        anon.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Con role Client
        await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"client-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Client",
            roles: [Roles.Client]);
        var asClient = await client.PostAsJsonAsync("/api/admin/invitations", new { maxUses = 1 });
        asClient.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
