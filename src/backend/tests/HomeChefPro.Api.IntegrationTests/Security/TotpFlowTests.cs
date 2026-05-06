using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Helpers;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Commands.Login2fa;
using HomeChefPro.Application.Auth.Commands.Totp;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Infrastructure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeChefPro.Api.IntegrationTests.Security;

/// <summary>
/// F-17 (Tier 3): MFA TOTP. Verifica el flujo completo end-to-end.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class TotpFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public TotpFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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
                    ["RateLimiting:Disabled"]       = "true",
                });
            });
        });
    }
    [Fact]
    public async Task Login_should_return_full_jwt_when_2fa_not_enabled()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        // Register sin activar 2FA
        var auth = await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"no2fa-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "User Sin 2FA",
            roles: ["Client"]);

        // Login fresco -> full JWT, no requires2fa
        client.DefaultRequestHeaders.Authorization = null;
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = auth.Email, password = IdentityTestHelpers.DefaultPassword });
        loginResp.EnsureSuccessStatusCode();
        var result = await loginResp.Content.ReadFromJsonAsync<AuthResultDto>();

        result.Should().NotBeNull();
        result!.Requires2fa.Should().BeFalse(
            because: "F-17: si 2FA no esta habilitado, login devuelve JWT real directo");
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.PartialToken.Should().BeNull();
    }

    

    [Fact]
    public async Task Login2fa_should_reject_wrong_code()
    {
        await using var api = CreateApi();
        using var client = api.CreateClient();

        var auth = await IdentityTestHelpers.RegisterAndAuthenticateAsync(
            api, client,
            email: $"totp-bad-{Guid.NewGuid():N}@hcp.test",
            password: IdentityTestHelpers.DefaultPassword,
            fullName: "Totp Bad",
            roles: ["Client"]);

        // Setup + verify para activar 2FA
        var setupResp = await client.PostAsJsonAsync("/api/auth/2fa/setup", new { });
        setupResp.EnsureSuccessStatusCode();
        var setupBad = await setupResp.Content.ReadFromJsonAsync<TotpSetupResult>();
        var code1 = ComputeTotp(setupBad!.SharedKey);
        var verify1 = await client.PostAsJsonAsync("/api/auth/2fa/verify-setup", new { code = code1 });
        if (!verify1.IsSuccessStatusCode)
        {
            var bodyDiag = await verify1.Content.ReadAsStringAsync();
            throw new System.Exception($"verify-setup failed status={verify1.StatusCode} body={bodyDiag} code1={code1}");
        }

        // Login step 1 -> partial
        client.DefaultRequestHeaders.Authorization = null;
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
            new { email = auth.Email, password = IdentityTestHelpers.DefaultPassword });
        var step1 = await loginResp.Content.ReadFromJsonAsync<AuthResultDto>();
        step1.Should().NotBeNull();
        step1!.Requires2fa.Should().BeTrue("verify-setup activo 2FA, login debe devolver partial");
        step1.PartialToken.Should().NotBeNullOrEmpty();

        // Step 2 con codigo invalido -> 401
        var step2Resp = await client.PostAsJsonAsync("/api/auth/2fa/login",
            new { partialToken = step1!.PartialToken, code = "000000" });
        step2Resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "F-17: codigo TOTP invalido debe rechazarse");
    }

    [Fact]
    public async Task Setup_2fa_should_require_authentication()
    {
        await using var api = CreateApi();
        using var anon = api.CreateClient();

        var resp = await anon.PostAsJsonAsync("/api/auth/2fa/setup", new { });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            because: "F-17: setup 2FA requiere autenticacion previa");
    }

    /// <summary>
    /// Computa un codigo TOTP segun RFC 6238 (SHA1, 30s, 6 digitos) a partir
    /// de la base32 secret. Replicate del algoritmo que ASP.NET Identity usa
    /// internamente, sin depender de UserManager.
    /// </summary>
    private static string ComputeTotp(string base32Secret, DateTimeOffset? at = null)
    {
        var t = (long)Math.Floor((at ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds() / 30.0);
        var counter = new byte[8];
        for (int i = 7; i >= 0; i--) { counter[i] = (byte)(t & 0xFF); t >>= 8; }
        var key = Base32Decode(base32Secret);
        using var hmac = new System.Security.Cryptography.HMACSHA1(key);
        var hash = hmac.ComputeHash(counter);
        int offset = hash[^1] & 0xF;
        int binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);
        int code = binary % 1_000_000;
        return code.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var clean = base32.TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(clean.Length * 5 / 8);
        int buffer = 0, bitsLeft = 0;
        foreach (var c in clean)
        {
            int idx = alphabet.IndexOf(c);
            if (idx < 0) continue;
            buffer = (buffer << 5) | idx;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }
        return bytes.ToArray();
    }
}
