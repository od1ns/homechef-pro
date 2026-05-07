using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Helpers;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Commands.RegisterUser;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace HomeChefPro.Api.IntegrationTests.Security;

/// <summary>
/// Tests de regresion para los hallazgos del audit Pasada A y Pasada B.
/// - F-01 docker env (no testeable a nivel app — verificar por CI grep).
/// - F-02 /uploads sin auth -> endpoint autenticado /api/uploads/{chefId}/payment-proofs/{filename}.
/// - F-03 Jwt:SigningKey con placeholder -> rechazado al startup.
/// - F-21 register acepta roles -> ignorado.
/// - F-22 SubmitPayment AmountUsd debe matchear order.TotalUsd.
/// - F-25 SubmitPayment rechaza re-submit si order paso PendingPayment.
/// - F-27 SubmitPayment valida coherencia AmountUsd / AmountPaidCurrency / ExchangeRate.
/// - F-32 Webhook delivery sin secret -> 401 (no aceptado como "no verificado").
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
                    ["Bootstrap:RequireInvitationCode"] = "false",
                    ["RateLimiting:Disabled"]      = "true",
                };
                configureExtra?.Invoke(dict);
                cfg.AddInMemoryCollection(dict);
            });
        });
    }

    // =========================================================================================
    // F-03: Jwt:SigningKey rechaza vacios/placeholders/short
    // =========================================================================================

    [Fact]
    public void Startup_should_reject_empty_jwt_signing_key()
    {
        var act = () =>
        {
            using var api = CreateApi(useTestAuth: false, configureExtra: d => d["Jwt:SigningKey"] = "");
            using var _ = api.CreateClient();
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
    // F-02: GET /api/uploads/{chefId}/payment-proofs/{filename} (Pasada C / H-05)
    // =========================================================================================

    [Fact]
    public async Task PaymentProof_get_should_reject_anonymous_with_401()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();
        var filename = $"{Guid.NewGuid():N}.png";
        var chefId = HomeChefPro.Domain.Tenancy.Chef.PilotoId;
        var resp = await client.GetAsync($"/api/uploads/{chefId:N}/payment-proofs/{filename}");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PaymentProof_get_should_reject_client_role_with_403()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();
        await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"client-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Cliente Test",
            roles: [Roles.Client]);

        var filename = $"{Guid.NewGuid():N}.png";
        var chefId = HomeChefPro.Domain.Tenancy.Chef.PilotoId;
        var resp = await client.GetAsync($"/api/uploads/{chefId:N}/payment-proofs/{filename}");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PaymentProof_get_should_reject_path_traversal_with_404()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();
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
            $"{Guid.NewGuid():N}.exe",
            $"{Guid.NewGuid():N}",
        };
        var chefId = HomeChefPro.Domain.Tenancy.Chef.PilotoId;
        foreach (var name in badNames)
        {
            var resp = await client.GetAsync($"/api/uploads/{chefId:N}/payment-proofs/{name}");
            resp.StatusCode.Should().Be(HttpStatusCode.NotFound,
                because: $"filename invalido o malicioso debe ser 404 — caso: {name}");
        }
    }

    [Fact]
    public async Task PaymentProof_get_should_succeed_for_cashier_when_file_exists()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();
        var bytes = new byte[]
        {
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
            // Pasada C / H-05: URL ahora incluye chef_id como prefix.
            uploadDto!.Url.Should().Contain("/payment-proofs/");

            await IdentityTestHelpers.RegisterAndAuthenticateAsync(
                api, client,
                email: $"cashier-ok-{Guid.NewGuid():N}@hcp.test",
                password: IdentityTestHelpers.DefaultPassword,
                fullName: "Cashier OK",
                roles: [Roles.Cashier]);

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
    // F-21: register debe ignorar Roles del body
    // =========================================================================================

    [Fact]
    public async Task Register_should_ignore_roles_field_from_body()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        var email = $"attacker-{Guid.NewGuid():N}@hcp.test";
        var reg = await client.PostAsJsonAsync("/api/auth/register", new RegisterUserCommand(
            Email: email,
            Password: IdentityTestHelpers.DefaultPassword,
            FullName: "Attacker",
            Roles: [Roles.Admin]));
        reg.EnsureSuccessStatusCode();
        var auth = (await reg.Content.ReadFromJsonAsync<AuthResultDto>())!;

        auth.Roles.Should().NotContain(Roles.Admin,
            because: "el endpoint debe ignorar el campo Roles del body (F-21 BOPLA)");
        auth.Roles.Should().Contain(Roles.Client,
            because: "el handler asigna Client por default cuando no llegan roles");
    }

    // =========================================================================================
    // F-22, F-25, F-27 (audit Pasada B) — SubmitPaymentProof validations
    // F-32 (audit Pasada B) — Webhook delivery fail-closed
    // =========================================================================================

    /// <summary>Helper: crea admin, dish, anon order. Devuelve orderId, totalUsd, clientes.</summary>
    private async Task<(Guid orderId, decimal totalUsd, HttpClient admin, HttpClient anon)>
        SetupOrderForPaymentTests(WebApplicationFactory<Program> factory, decimal dishPrice)
    {
        var adminClient = factory.CreateClient();
        await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            factory, adminClient,
            email: $"admin-pay-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Admin Pay",
            roles: [Roles.Admin]);

        var dishResp = await adminClient.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand(Name: $"Test {Guid.NewGuid():N}", SellingPriceUsd: dishPrice));
        dishResp.EnsureSuccessStatusCode();
        var dishId = (await dishResp.Content.ReadFromJsonAsync<_TestIdResponse>())!.Id;

        var anonClient = factory.CreateClient();
        var orderResp = await anonClient.PostAsJsonAsync("/api/client/orders",
            new CreateGuestOrderCommand(
                GuestFullName: "Test Client",
                GuestPhone: "+58 412-000-9999",
                DeliveryType: "pickup",
                Items: [new OrderLineInput(dishId, 1)]));
        orderResp.EnsureSuccessStatusCode();
        // F-24: el response es {id, accessToken}; solo necesitamos el id aqui.
        var orderId = (await orderResp.Content.ReadFromJsonAsync<_TestIdResponse>())!.Id;
        return (orderId, dishPrice, adminClient, anonClient);
    }

    [Fact]
    public async Task F22_Submit_payment_with_wrong_amount_should_be_rejected_with_409()
    {
        await using var api = CreateApi();
        var (orderId, _, _, anon) = await SetupOrderForPaymentTests(api, dishPrice: 10m);

        var resp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(
                OrderId: orderId,
                Method: "pago_movil",
                AmountUsd: 0.01m,           // BUG: deberia matchear order.TotalUsd = 10
                PaidCurrency: "VES",
                AmountPaidCurrency: 0.40m,
                ExchangeRateUsed: 40m));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict,
            because: "F-22: AmountUsd debe coincidir con order.TotalUsd");
    }

    [Fact]
    public async Task F22_Submit_payment_with_correct_amount_should_succeed()
    {
        await using var api = CreateApi();
        var (orderId, totalUsd, _, anon) = await SetupOrderForPaymentTests(api, dishPrice: 10m);

        var resp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(
                OrderId: orderId,
                Method: "pago_movil",
                AmountUsd: totalUsd,
                PaidCurrency: "VES",
                AmountPaidCurrency: totalUsd * 40,
                ExchangeRateUsed: 40m));

        resp.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "happy path con monto correcto debe crear el payment");
    }

    [Fact]
    public async Task F25_Submit_second_payment_after_order_paid_should_be_rejected_with_409()
    {
        await using var api = CreateApi();
        var (orderId, totalUsd, admin, anon) = await SetupOrderForPaymentTests(api, dishPrice: 10m);

        // Primer payment legitimo.
        var first = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(
                OrderId: orderId,
                Method: "pago_movil",
                AmountUsd: totalUsd,
                PaidCurrency: "VES",
                AmountPaidCurrency: totalUsd * 40,
                ExchangeRateUsed: 40m));
        first.EnsureSuccessStatusCode();
        var firstPayId = (await first.Content.ReadFromJsonAsync<_TestIdResponse>())!.Id;

        // Admin verifica → order avanza a paid.
        var verify = await admin.PostAsync($"/api/admin/payments/{firstPayId}/verify", null);
        verify.EnsureSuccessStatusCode();

        // F-25: re-submit en estado != PendingPayment debe ser rechazado.
        var second = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(
                OrderId: orderId,
                Method: "pago_movil",
                AmountUsd: totalUsd,
                PaidCurrency: "VES",
                AmountPaidCurrency: totalUsd * 40,
                ExchangeRateUsed: 40m));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict,
            because: "F-25: order ya no esta en PendingPayment, no se aceptan nuevos comprobantes");
    }

    [Fact]
    public async Task F27_Submit_payment_with_inconsistent_VES_rate_should_be_rejected_with_409()
    {
        await using var api = CreateApi();
        var (orderId, _, _, anon) = await SetupOrderForPaymentTests(api, dishPrice: 10m);

        // F-27: AmountUsd=10, AmountPaidCurrency=1 VES, ExchangeRate=50 VES/USD.
        // Derived: 1/50 = 0.02 USD, NO matchea AmountUsd 10 → reject.
        var resp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(
                OrderId: orderId,
                Method: "pago_movil",
                AmountUsd: 10m,
                PaidCurrency: "VES",
                AmountPaidCurrency: 1m,
                ExchangeRateUsed: 50m));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict,
            because: "F-27: ratio VES/USD inconsistente debe ser rechazado");
    }

    [Fact]
    public async Task F32_Webhook_without_secret_configured_should_be_rejected_with_401()
    {
        // Default config: DeliveryWebhooks:Secrets:* NO seteado en el factory de test.
        // RejectInvalidSignature default = true → endpoint debe responder 401.
        await using var api = CreateApi();
        using var anon = api.CreateClient();

        var body = new StringContent(
            "{\"status\":\"delivered\",\"orderId\":\"00000000-0000-0000-0000-000000000001\"}",
            System.Text.Encoding.UTF8,
            "application/json");

        var resp = await anon.PostAsync("/api/webhooks/delivery/yummy", body);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "F-32: webhook sin secret configurado debe rechazarse, no aceptarse como 'no verificado'");
    }

    // =========================================================================================
    // Tier 2: F-09 magic bytes, F-16 logout requires auth, F-31 limites en items
    // =========================================================================================

    // F-09: payload con Content-Type "image/jpeg" pero body NO-imagen debe rechazarse.
    [Fact]
    public async Task PaymentProof_upload_should_reject_non_image_content()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // body es texto plano "<html>...</html>" — no matchea magic bytes JPEG/PNG/WebP.
        var bytes = System.Text.Encoding.UTF8.GetBytes("<html><body>I am not a JPEG</body></html>");
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", "fake.jpg");

        var resp = await client.PostAsync("/api/uploads/payment-proofs", multipart);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "F-09: el header dice JPEG pero el body es HTML — magic bytes deben rechazar");
    }

    // F-09: bytes con firma JPEG correcta deben aceptarse aunque el filename sea otra cosa.
    [Fact]
    public async Task PaymentProof_upload_should_accept_valid_jpeg_magic_bytes()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // FF D8 FF E0 + relleno minimo. Es un JPEG header valido (SOI marker).
        var bytes = new byte[]
        {
            0xFF, 0xD8, 0xFF, 0xE0,
            0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
            0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00,
        };
        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", "ok.jpg");

        var resp = await client.PostAsync("/api/uploads/payment-proofs", multipart);
        resp.StatusCode.Should().Be(HttpStatusCode.OK,
            because: "F-09: bytes JPEG con SOI marker FF D8 FF deben pasar el check");
    }

    // F-16: logout debe exigir auth. Antes era AllowAnonymous (un atacante con
    // refresh token robado podia revocarlo y dejar al usuario sin sesion).
    [Fact]
    public async Task Logout_should_require_authentication()
    {
        await using var api = CreateApi();
        using var anon = api.CreateClient();

        var resp = await anon.PostAsJsonAsync("/api/auth/logout",
            new { refreshToken = "no-importa-cual-sea" });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "F-16: logout sin Authorization header debe responder 401");
    }

    // F-31: orden con > 30 items distintos debe rechazarse.
    [Fact]
    public async Task CreateGuestOrder_should_reject_more_than_30_items()
    {
        await using var api = CreateApi();
        using var anon = api.CreateClient();

        // Construimos un payload con 31 OrderLineInput inventados (ids al azar).
        var items = Enumerable.Range(0, 31)
            .Select(_ => new { dishId = Guid.NewGuid(), quantity = 1 })
            .ToArray();
        var resp = await anon.PostAsJsonAsync("/api/client/orders", new
        {
            guestFullName = "Tester F31",
            guestPhone = "+58 412-555-9999",
            deliveryType = "pickup",
            items,
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "F-31: max 30 items distintos por orden");
    }

    // F-31: quantity > 50 por item debe rechazarse.
    [Fact]
    public async Task CreateGuestOrder_should_reject_quantity_greater_than_50()
    {
        await using var api = CreateApi();
        using var anon = api.CreateClient();

        var resp = await anon.PostAsJsonAsync("/api/client/orders", new
        {
            guestFullName = "Tester F31q",
            guestPhone = "+58 412-555-9990",
            deliveryType = "pickup",
            items = new[] { new { dishId = Guid.NewGuid(), quantity = 51 } },
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            because: "F-31: quantity max por item es 50");
    }

    // ---------------------------------------------------------------------
    // Helper records
    // ---------------------------------------------------------------------

    private sealed record UploadResponse(string Url, string ContentType, long SizeBytes);
    private sealed record _TestIdResponse(Guid Id);
}
