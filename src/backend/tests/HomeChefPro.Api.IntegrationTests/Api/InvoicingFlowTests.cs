using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HomeChefPro.Api.Endpoints;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Invoicing.Commands.EmitInvoice;
using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using HomeChefPro.Api.IntegrationTests.Helpers;

namespace HomeChefPro.Api.IntegrationTests.Api;

[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class InvoicingFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public InvoicingFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

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
                    ["RateLimiting:Disabled"]      = "true",
                ["Issuer:Rif"]                  = "J-12345678-9",
                ["Issuer:LegalName"]            = "Cocina HCP, C.A.",
                ["Issuer:Address"]              = "Av Principal, Caracas",
            }));
        });

    private static async Task<HttpClient> AdminClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var auth = await IdentityTestHelpers.RegisterAndAssignRolesAsync(
            factory, client, $"inv-{Guid.NewGuid():N}@hcp.test", "Test1234", "Invoice Admin", [Roles.Admin]);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    [Fact]
    public async Task Issue_invoice_calculates_iva_and_marks_issued_with_mock_numbers()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        // Pickup order: paid in VES → IGTF should NOT apply.
        var dish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand(Name: $"Plato fact {Guid.NewGuid():N}", SellingPriceUsd: 10m));
        var dishId = (await dish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using var anon = factory.CreateClient();
        var orderResp = await anon.PostAsJsonAsync("/api/client/orders", new CreateGuestOrderCommand(
            GuestFullName: "Cliente Factura",
            GuestPhone: "+58 412-555-7000",
            DeliveryType: "pickup",
            Items: [new OrderLineInput(dishId, 2)]));
        var orderId = (await orderResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var payResp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(orderId, "pago_movil", 20m, "VES", 800m, 40m));
        var payId = (await payResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);
        foreach (var step in new[] { "in_preparation", "ready", "delivered" })
            await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                new AdminOrdersEndpoints.AdvanceRequest(step));

        // Issue invoice.
        var emitResp = await admin.PostAsJsonAsync("/api/admin/invoices",
            new EmitInvoiceCommand(orderId, CustomerLegalName: "Cliente Factura"));
        emitResp.EnsureSuccessStatusCode();
        var inv = (await emitResp.Content.ReadFromJsonAsync<InvoiceDto>())!;

        inv.Status.Should().Be("issued");
        inv.SubtotalUsd.Should().Be(20m);
        inv.IvaUsd.Should().Be(3.2m);             // 16% × 20
        inv.IgtfUsd.Should().Be(0m);              // VES → no IGTF
        inv.TotalWithTaxUsd.Should().Be(23.2m);
        inv.IgtfApplies.Should().BeFalse();
        inv.FiscalNumber.Should().StartWith("MOCK-");
        inv.ControlNumber.Should().StartWith("CTRL-");
        inv.IssuerRif.Should().Be("J-12345678-9");
    }

    [Fact]
    public async Task Issue_with_zelle_payment_applies_igtf()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        var dish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand(Name: $"Plato Zelle {Guid.NewGuid():N}", SellingPriceUsd: 10m));
        var dishId = (await dish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using var anon = factory.CreateClient();
        var orderResp = await anon.PostAsJsonAsync("/api/client/orders", new CreateGuestOrderCommand(
            GuestFullName: "Zelle Customer",
            GuestPhone: "+58 412-555-7001",
            DeliveryType: "pickup",
            Items: [new OrderLineInput(dishId, 1)]));
        var orderId = (await orderResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        var payResp = await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(orderId, "zelle", 10m, "USD", 10m));
        var payId = (await payResp.Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);
        foreach (var step in new[] { "in_preparation", "ready", "delivered" })
            await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                new AdminOrdersEndpoints.AdvanceRequest(step));

        var inv = (await (await admin.PostAsJsonAsync("/api/admin/invoices",
            new EmitInvoiceCommand(orderId)))
            .Content.ReadFromJsonAsync<InvoiceDto>())!;

        inv.Status.Should().Be("issued");
        inv.SubtotalUsd.Should().Be(10m);
        inv.IvaUsd.Should().Be(1.6m);
        inv.IgtfUsd.Should().Be(0.3m);            // 3% × 10
        inv.IgtfApplies.Should().BeTrue();
        inv.TotalWithTaxUsd.Should().Be(11.9m);
    }

    [Fact]
    public async Task Receipt_pdf_for_invoiced_order_includes_fiscal_marker()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        var dish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand(Name: $"Plato fiscal {Guid.NewGuid():N}", SellingPriceUsd: 8m));
        var dishId = (await dish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using var anon = factory.CreateClient();
        var orderId = (await (await anon.PostAsJsonAsync("/api/client/orders",
            new CreateGuestOrderCommand("Cliente PDF Fiscal", "+58 412-555-7100", "pickup",
                [new OrderLineInput(dishId, 1)])))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var payId = (await (await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(orderId, "pago_movil", 8m, "VES", 320m, 40m)))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);
        foreach (var step in new[] { "in_preparation", "ready", "delivered" })
            await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                new AdminOrdersEndpoints.AdvanceRequest(step));

        var inv = (await (await admin.PostAsJsonAsync("/api/admin/invoices",
            new EmitInvoiceCommand(orderId, CustomerLegalName: "ACME C.A.")))
            .Content.ReadFromJsonAsync<InvoiceDto>())!;
        inv.Status.Should().Be("issued");

        var pdfResp = await admin.GetAsync($"/api/admin/orders/{orderId}/receipt.pdf");
        pdfResp.EnsureSuccessStatusCode();
        var bytes = await pdfResp.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(800);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).Should().Be("%PDF-");

        var disposition = pdfResp.Content.Headers.ContentDisposition?.FileName
            ?? pdfResp.Content.Headers.ContentDisposition?.FileNameStar;
        disposition.Should().NotBeNullOrEmpty();
        disposition!.Should().Contain("factura-").And.Contain(inv.FiscalNumber!);
    }

    [Fact]
    public async Task Cancel_issued_invoice_changes_status_with_reason()
    {
        await using var factory = CreateApi();
        using var admin = await AdminClient(factory);

        var dish = await admin.PostAsJsonAsync("/api/admin/recipes/dishes",
            new CreateDishCommand(Name: $"Plato anular {Guid.NewGuid():N}", SellingPriceUsd: 5m));
        var dishId = (await dish.Content.ReadFromJsonAsync<IdResponse>())!.Id;

        using var anon = factory.CreateClient();
        var orderId = (await (await anon.PostAsJsonAsync("/api/client/orders",
            new CreateGuestOrderCommand("X", "+58 412-555-7002", "pickup", [new OrderLineInput(dishId, 1)])))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;
        var payId = (await (await anon.PostAsJsonAsync($"/api/client/orders/{orderId}/payment",
            new SubmitPaymentProofCommand(orderId, "pago_movil", 5m, "VES", 200m, 40m)))
            .Content.ReadFromJsonAsync<IdResponse>())!.Id;
        await admin.PostAsync($"/api/admin/payments/{payId}/verify", null);
        foreach (var step in new[] { "in_preparation", "ready", "delivered" })
            await admin.PostAsJsonAsync($"/api/admin/orders/{orderId}/advance",
                new AdminOrdersEndpoints.AdvanceRequest(step));

        var inv = (await (await admin.PostAsJsonAsync("/api/admin/invoices",
            new EmitInvoiceCommand(orderId))).Content.ReadFromJsonAsync<InvoiceDto>())!;

        var cancelResp = await admin.PostAsJsonAsync(
            $"/api/admin/invoices/{inv.Id}/cancel",
            new AdminInvoicesEndpoints.CancelBody("Cliente devolvió el pedido"));
        cancelResp.EnsureSuccessStatusCode();

        var refreshed = await admin.GetFromJsonAsync<InvoiceDto>($"/api/admin/invoices/{inv.Id}");
        refreshed!.Status.Should().Be("cancelled");
        refreshed.CancellationReason.Should().Be("Cliente devolvió el pedido");
    }

    private sealed record IdResponse(Guid Id);
}
