using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Reports.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HomeChefPro.Api.IntegrationTests.Helpers;

namespace HomeChefPro.Api.IntegrationTests.Api;

/// <summary>
/// Tests integration de los 4 reportes agregados en sprints recientes:
/// inventory-rotation, customer-ranking, peak-hours-heatmap, peak-hours-summary.
/// Verifican que los endpoints responden 200 con un array tipado.
/// Tests de logica de negocio mas profunda (valores especificos, segmentacion
/// RFM, distribucion del heatmap) requeririan manipulacion de timestamps de
/// orders, lo cual es out-of-scope para tests de smoke. Los reportes ya estan
/// cubiertos por seed-rich-history.ps1 en demos manuales.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class ReportsAdditionalTests
{
    private readonly LiveDatabaseFixture _fixture;

    public ReportsAdditionalTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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

    private static async Task<HttpClient> AdminClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var auth = await IdentityTestHelpers.RegisterAndAssignRolesAsync(
            factory, client, $"reports-extra-{Guid.NewGuid():N}@hcp.test", "Test1234", "Reports Extra Admin", [Roles.Admin]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Inventory_rotation_returns_array_for_admin()
    {
        await using var factory = CreateApi();
        var admin = await AdminClient(factory);

        var resp = await admin.GetAsync("/api/admin/reports/inventory-rotation");
        resp.EnsureSuccessStatusCode();

        var rows = await resp.Content.ReadFromJsonAsync<InventoryRotationRow[]>();
        rows.Should().NotBeNull();
        // El seed crea ingredientes; cada uno debe tener una row con su categoria
        // de rotacion ("hot" / "warm" / "cold" / "stale" / "no-data").
        rows!.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.RotationCategory));
        rows!.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.UseUnit));
    }

    [Fact]
    public async Task Customer_ranking_returns_array_for_admin()
    {
        await using var factory = CreateApi();
        var admin = await AdminClient(factory);

        var resp = await admin.GetAsync("/api/admin/reports/customer-ranking");
        resp.EnsureSuccessStatusCode();

        var rows = await resp.Content.ReadFromJsonAsync<CustomerRankingRow[]>();
        rows.Should().NotBeNull();
        // Si hay clientes con ordenes (admin/registered/guest), aparecen aca.
        // Cada uno debe tener segment definido y customer_type valido.
        rows!.Should().OnlyContain(r => !string.IsNullOrWhiteSpace(r.Segment));
        rows!.Should().OnlyContain(r =>
            r.CustomerType == "registered" || r.CustomerType == "guest");
    }

    [Fact]
    public async Task Peak_hours_heatmap_returns_array_for_admin()
    {
        await using var factory = CreateApi();
        var admin = await AdminClient(factory);

        var resp = await admin.GetAsync("/api/admin/reports/peak-hours-heatmap");
        resp.EnsureSuccessStatusCode();

        var rows = await resp.Content.ReadFromJsonAsync<PeakHourCellRow[]>();
        rows.Should().NotBeNull();
        // dia_semana es 0..6, hora 0..23
        rows!.Should().OnlyContain(r => r.DayOfWeek >= 0 && r.DayOfWeek <= 6);
        rows!.Should().OnlyContain(r => r.HourOfDay >= 0 && r.HourOfDay <= 23);
    }

    [Fact]
    public async Task Peak_hours_summary_returns_array_for_admin()
    {
        await using var factory = CreateApi();
        var admin = await AdminClient(factory);

        var resp = await admin.GetAsync("/api/admin/reports/peak-hours-summary");
        resp.EnsureSuccessStatusCode();

        var rows = await resp.Content.ReadFromJsonAsync<PeakHourSummaryRow[]>();
        rows.Should().NotBeNull();
        rows!.Should().OnlyContain(r => r.DayOfWeek >= 0 && r.DayOfWeek <= 6);
        rows!.Should().OnlyContain(r => r.PeakHour >= 0 && r.PeakHour <= 23);
    }
}
