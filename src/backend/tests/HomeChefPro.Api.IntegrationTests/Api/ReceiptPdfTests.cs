using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using HomeChefPro.Api.IntegrationTests.Helpers;

namespace HomeChefPro.Api.IntegrationTests.Api;

[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class ReceiptPdfTests
{
    private readonly LiveDatabaseFixture _fixture;

    public ReceiptPdfTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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
            }));
        });

    private static async Task<HttpClient> AdminClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var auth = await IdentityTestHelpers.RegisterAndAssignRolesAsync(
            factory, client, $"receipt-admin-{Guid.NewGuid():N}@hcp.test", "Test1234", "Receipt Admin", [Roles.Admin]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Client_can_download_receipt_pdf_for_their_order()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        // Seed a dish via admin
        var createDish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes", new CreateDishCommand(
            Name: $"Plato recibo {Guid.NewGuid():N}",
            SellingPriceUsd: 7m,
            PrepTimeMinutes: 10));
        createDish.EnsureSuccessStatusCode();
        var dishId = (await createDish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // Anon client creates guest order
        using var anon = factory.CreateClient();
        var orderResp = await anon.PostAsJsonAsync("/api/client/orders", new CreateGuestOrderCommand(
            GuestFullName: "Cliente Recibo",
            GuestPhone: "+58 412-111-0001",
            DeliveryType: "pickup",
            Items: [new OrderLineInput(dishId, 2)]));
        orderResp.EnsureSuccessStatusCode();
        var orderId = (await orderResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        // Download receipt (anon, same as tracking)
        var pdfResp = await anon.GetAsync($"/api/client/orders/{orderId}/receipt.pdf");
        pdfResp.StatusCode.Should().Be(HttpStatusCode.OK);
        pdfResp.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");

        var bytes = await pdfResp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(500);   // Any real PDF is well over this.
        Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");

        // Content-Disposition should carry the filename with the order number.
        var disposition = pdfResp.Content.Headers.ContentDisposition?.FileName
                          ?? pdfResp.Content.Headers.ContentDisposition?.FileNameStar;
        disposition.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Admin_receipt_endpoint_requires_auth()
    {
        await using var factory = CreateApi();
        using var anon = factory.CreateClient();

        var resp = await anon.GetAsync($"/api/admin/orders/{Guid.NewGuid()}/receipt.pdf");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record IdResponse(Guid Id);
}
