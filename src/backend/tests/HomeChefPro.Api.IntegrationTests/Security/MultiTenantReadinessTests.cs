using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Helpers;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;
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
/// Pasada C / Fase 1C-A — tests de readiness multi-tenant que verifican
/// que cada bloque del audit haya quedado realmente aplicado en runtime:
///
///   - Bloque 1 (schema): chef piloto seedeado, chef_id en tablas.
///   - Bloque 2 (Domain): nuevas entities reciben ChefId=piloto via SQL DEFAULT.
///   - Bloque 3 (JWT): el access token contiene claim "chef_id".
///   - Bloque 4 (Issuer): facturas toman RIF del chef, no de appsettings.
///   - Bloque 5 (Uploads): URL incluye chef_id como prefix.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class MultiTenantReadinessTests
{
    private readonly LiveDatabaseFixture _fixture;

    public MultiTenantReadinessTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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

    // ---------------------------------------------------------------------
    // Bloque 1: schema con tabla chefs y chef_id en tablas de negocio
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Chef_pilot_should_exist_in_database_with_realistic_rif()
    {
        await using var api = CreateApi();
        using var _ = api.CreateClient(); // dispara host startup
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();

        var pilot = await db.Chefs.FindAsync(Chef.PilotoId);

        pilot.Should().NotBeNull("Pasada C / H-01: el chef piloto debe estar seedeado en 01b_chefs.sql");
        pilot!.Rif.Should().Be("J-12345678-9", "Pasada C / H-03: RIF SENIAT valido del piloto");
        pilot.LegalName.Should().Be("Cocina HCP, C.A.");
        pilot.InvoicePrefix.Should().Be("HC", "el correlativo del piloto sigue siendo HC-YYYYMMDD-NNNN");
        pilot.Status.Should().Be(ChefStatus.Active);
    }

    // ---------------------------------------------------------------------
    // Bloque 2: nuevas entities heredan ChefId del piloto via SQL DEFAULT
    // ---------------------------------------------------------------------

    [Fact]
    public async Task New_ingredient_should_default_to_pilot_chef_id_via_sql_default()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"admin-mt-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Admin Tenant",
            roles: [Roles.Admin]);

        // Crear ingredient via MediatR -> EF inserta sin chef_id (sentinel
        // Guid.Empty), Postgres aplica DEFAULT del piloto, EF re-lee.
        using (var scope = api.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var ingredientId = await mediator.Send(new CreateIngredientCommand(
                Name: $"Test Tomate {Guid.NewGuid():N}",
                UseUnit: "g"));

            var db = scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();
            var saved = await db.Ingredients.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == ingredientId);
            saved.Should().NotBeNull();
            saved!.ChefId.Should().Be(Chef.PilotoId,
                because: "Pasada C / H-01: SQL DEFAULT del piloto debe llenar chef_id en single-tenant");
        }
    }

    // ---------------------------------------------------------------------
    // Bloque 3: JWT incluye claim chef_id
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Login_should_emit_jwt_with_chef_id_claim()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Register lo crea como Client (F-21) y devuelve token.
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterUserCommand(
                Email: $"jwt-test-{Guid.NewGuid():N}@hcp.test",
                Password: IdentityTestHelpers.DefaultPassword,
                FullName: "JWT Test"));
        resp.EnsureSuccessStatusCode();
        var auth = await resp.Content.ReadFromJsonAsync<AuthResultDto>();
        auth.Should().NotBeNull();

        // Decode JWT (no validamos firma, solo extraemos claims).
        var parts = auth!.AccessToken.Split('.');
        parts.Should().HaveCount(3, "JWT estandar: header.payload.signature");
        var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        using var doc = JsonDocument.Parse(payloadJson);

        doc.RootElement.TryGetProperty("chef_id", out var chefIdProp).Should().BeTrue(
            because: "Pasada C / H-04: cada JWT debe llevar el tenant del usuario firmado");
        var chefIdValue = chefIdProp.GetString();
        Guid.Parse(chefIdValue!).Should().Be(Chef.PilotoId,
            because: "single-tenant: todos los users hoy apuntan al piloto");
    }

    // ---------------------------------------------------------------------
    // Bloque 5: URL del upload incluye chef_id
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Upload_url_should_include_pilot_chef_id_prefix()
    {
        await using var api = CreateApi();
        using var anonClient = api.CreateClient();

        var bytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 };
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", "ok.jpg");

        var upload = await anonClient.PostAsync("/api/uploads/payment-proofs", multipart);
        upload.EnsureSuccessStatusCode();
        var dto = await upload.Content.ReadFromJsonAsync<UploadResp>();
        dto.Should().NotBeNull();

        var pilotPrefix = $"/{Chef.PilotoId:N}/payment-proofs/";
        dto!.Url.Should().Contain(pilotPrefix,
            because: "Pasada C / H-05: la URL debe segregar archivos por chef_id");
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private static byte[] Base64UrlDecode(string s)
    {
        var p = s.Replace('-', '+').Replace('_', '/');
        switch (p.Length % 4) { case 2: p += "=="; break; case 3: p += "="; break; }
        return Convert.FromBase64String(p);
    }

    private sealed record UploadResp(string Url, string ContentType, long SizeBytes);
}
