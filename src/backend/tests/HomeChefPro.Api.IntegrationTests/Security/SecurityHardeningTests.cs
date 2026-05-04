using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Helpers;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HomeChefPro.Api.IntegrationTests.Security;

/// <summary>
/// Tests de regresion para los hallazgos del audit Pasada A
/// (docs/audits/audit-2026-05-03-A.md).
///
/// - F-01: docker-compose Development env (no testeable a nivel app — verificable por CI grep).
/// - F-02: <c>/uploads/*</c> servia archivos sin auth → ahora endpoint autenticado en
///   <c>/api/uploads/payment-proofs/{filename}</c>.
/// - F-03: <c>Jwt:SigningKey</c> con placeholder literal → rechazado al startup.
/// - F-21: <c>POST /api/auth/register</c> aceptaba <c>Roles</c> desde el body → ahora ignorado.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class SecurityHardeningTests
{
    private readonly LiveDatabaseFixture _fixture;

    public SecurityHardeningTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    private WebApplicationFactory<Program> CreateApi(
        Action<Dictionary<string, string?>>? configureExtra = null,
        bool useTestAuth = true)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.UseTestDatabase(_fixture.ConnectionString);
            // F-03 tests: para validar que la validacion rechace keys malas, hay que evitar
            // que UseTestAuth las sobrescriba con un PostConfigure. Los tests pasan
            // useTestAuth:false en esos casos.
            if (useTestAuth) b.UseTestAuth();
            b.ConfigureAppConfiguration((_, cfg) =>
            {
                var dict = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PostgreSQL"] = _fixture.ConnectionString,
                    ["ConnectionStrings:Redis"]     = "",
                    ["Jwt:Issuer"]                  = "HomeChefPro-Test",
                    ["Jwt:Audience"]                = "HomeChefPro-Clients-Test",
                    ["Jwt:SigningKey"]              = new string('x', 64),
                    ["Jwt:AccessTokenMinutes"]      = "60",
                    ["Bootstrap:EnableOnStart"]     = "false",
                };
                configureExtra?.Invoke(dict);
                cfg.AddInMemoryCollection(dict);
            });
        });
    }

    // =========================================================================================
    // F-03: validacion de Jwt:SigningKey rechaza placeholders/empty/short al startup
    // =========================================================================================

    [Fact]
    public void Startup_should_reject_empty_jwt_signing_key()
    {
        var act = () =>
        {
            using var api = CreateApi(useTestAuth: false, configureExtra: d => d["Jwt:SigningKey"] = "");
            using var _ = api.CreateClient(); // forces host build
        };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*Jwt:SigningKey is required*");
    }

    [Fact]
    public void Startup_should_reject_placeholder_jwt_signing_key()
    {
        var act = () =>
        {
            using var api = CreateApi(useTestAuth: false, configureExtra: d => d["Jwt:SigningKey"] =
                "REEMPLAZAR_EN_PRODUCCION_CON_SECRETO_LARGO_Y_ALEATORIO_DE_AL_MENOS_32_BYTES");
            using var _ = api.CreateClient();
        };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void Startup_should_reject_changeme_jwt_signing_key()
    {
        var act = () =>
        {
            using var api = CreateApi(useTestAuth: false, configureExtra: d => d["Jwt:SigningKey"] =
                "change-me-to-a-long-random-string-of-at-least-32-bytes");
            using var _ = api.CreateClient();
        };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*placeholder*");
    }

    [Fact]
    public void Startup_should_reject_too_short_jwt_signing_key()
    {
        var act = () =>
        {
            using var api = CreateApi(useTestAuth: false, configureExtra: d => d["Jwt:SigningKey"] = "short");
            using var _ = api.CreateClient();
        };

        act.Should().Throw<OptionsValidationException>()
            .WithMessage("*at least 32 characters*");
    }

    // =========================================================================================
    // F-02: GET /api/uploads/payment-proofs/{filename} requiere auth + rol Cashier/Admin
    // =========================================================================================

    [Fact]
    public async Task PaymentProof_get_should_reject_anonymous_with_401()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Filename con shape valido pero archivo no existente — la auth corre antes que la
        // existencia → debe responder 401, no 404.
        var filename = $"{Guid.NewGuid():N}.png";
        var resp = await client.GetAsync($"/api/uploads/payment-proofs/{filename}");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PaymentProof_get_should_reject_client_role_with_403()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Registrar un user con rol Client (default sin promocion).
        await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"client-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Cliente Test",
            roles: [Roles.Client]);

        var filename = $"{Guid.NewGuid():N}.png";
        var resp = await client.GetAsync($"/api/uploads/payment-proofs/{filename}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PaymentProof_get_should_reject_path_traversal_with_404()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Cashier autenticado para distinguir 401 (no auth) de 404 (filename invalido).
        await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"cashier-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Cashier Test",
            roles: [Roles.Cashier]);

        string[] badNames =
        {
            "../../../etc/passwd",
            "..%2F..%2Fetc%2Fpasswd",
            "valid-but-not-guid.png",
            $"{Guid.NewGuid():N}.exe",       // extension no permitida
            $"{Guid.NewGuid():N}",           // sin extension
        };

        foreach (var name in badNames)
        {
            var resp = await client.GetAsync($"/api/uploads/payment-proofs/{name}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: $"filename invalido o malicioso debe ser 404 — caso: {name}");
        }
    }

    [Fact]
    public async Task PaymentProof_get_should_succeed_for_cashier_when_file_exists()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // 1) Subir un comprobante (POST sigue siendo AllowAnonymous, intencional para guests).
        var bytes = new byte[]
        {
            // PNG magic + IHDR chunk header. Para este test alcanza con que el endpoint
            // acepte el upload y guarde el archivo en disco; magic-byte validation server-side
            // es F-09, pendiente.
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
        };

        using (var anonClient = api.CreateClient())
        using (var multipart = new MultipartFormDataContent())
        {
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            multipart.Add(fileContent, "file", "test.png");

            var upload = await anonClient.PostAsync("/api/uploads/payment-proofs", multipart);
            upload.EnsureSuccessStatusCode();
            var uploadDto = await upload.Content.ReadFromJsonAsync<UploadResponse>();
            uploadDto.Should().NotBeNull();
            uploadDto!.Url.Should().StartWith("/api/uploads/payment-proofs/");

            // 2) Cashier autenticado.
            await IdentityTestHelpers.RegisterAndAuthenticateAsync(
                api, client,
                email: $"cashier-ok-{Guid.NewGuid():N}@hcp.test",
                password: IdentityTestHelpers.DefaultPassword,
                fullName: "Cashier OK",
                roles: [Roles.Cashier]);

            // 3) GET del comprobante recien subido — 200 + headers de defense in depth.
            var resp = await client.GetAsync(uploadDto.Url);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            resp.Content.Headers.ContentType?.MediaType.Should().Be("image/png");
            resp.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
            resp.Headers.GetValues("Cache-Control")
                .SelectMany(v => v.Split(','))
                .Select(p => p.Trim())
                .Should().Contain(p => string.Equals(p, "no-store", StringComparison.OrdinalIgnoreCase));
        }
    }

    // =========================================================================================
    // F-21: POST /api/auth/register debe ignorar el campo Roles del body
    // =========================================================================================

    [Fact]
    public async Task Register_should_ignore_roles_field_from_body()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Atacante registra con Roles=[Admin] en el body. El endpoint debe IGNORAR ese campo
        // y crear un user con rol Client.
        var email = $"attacker-{Guid.NewGuid():N}@hcp.test";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: email,
            Password: IdentityTestHelpers.DefaultPassword,
            FullName: "Attacker",
            Roles: [Roles.Admin]));
        reg.EnsureSuccessStatusCode();
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResultDto>())!;

        // El JWT retornado NO debe contener Admin en las claims.
        auth.Roles.Should().NotContain(Roles.Admin,
            because: "el endpoint debe ignorar el campo Roles del body (F-21 BOPLA)");
        auth.Roles.Should().Contain(Roles.Client,
            because: "el handler asigna Client por default cuando no llegan roles");

        // Defense in depth: intentar usar el token para llamar un endpoint admin → 403.
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var adminCall = await client.GetAsync("/api/admin/ingredients");
        adminCall.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.Unauthorized);
    }

    private sealed record UploadResponse(string Url, string ContentType, long SizeBytes);
}
