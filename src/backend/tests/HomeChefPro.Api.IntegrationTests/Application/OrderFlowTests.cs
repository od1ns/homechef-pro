using FluentAssertions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Orders.Commands.AdvanceOrderStatus;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Orders.Queries.GetOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Application.Payments.Commands.VerifyPayment;

namespace HomeChefPro.Api.IntegrationTests.Application;

[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class OrderFlowTests
{
    private readonly LiveDatabaseFixture _fixture;

    public OrderFlowTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Full_happy_path_from_order_to_delivered()
    {
        var adminId = Guid.NewGuid();
        await using var host = new ApplicationTestHost(_fixture, currentUserId: adminId);

        var dishId = await host.Mediator.Send(new CreateDishCommand(
            Name: "Arepa de prueba",
            SellingPriceUsd: 5m,
            PrepTimeMinutes: 10));

        var orderId = await host.Mediator.Send(new CreateGuestOrderCommand(
            GuestFullName: "Test Client",
            GuestPhone: "+58 412-555-0100",
            DeliveryType: "pickup",
            Items: [new OrderLineInput(dishId, 2)]));

        var placed = await host.Mediator.Send(new GetOrderQuery(orderId));
        placed.Status.Should().Be("pending_payment");
        placed.OrderNumber.Should().StartWith("HC-");
        placed.TotalUsd.Should().Be(10m);

        // Client submits payment proof.
        var payId = await host.Mediator.Send(new SubmitPaymentProofCommand(
            OrderId: orderId,
            Method: "pago_movil",
            AmountUsd: 10m,
            PaidCurrency: "VES",
            AmountPaidCurrency: 400m,
            ExchangeRateUsed: 40m,
            ReferenceNumber: "REF-001"));

        (await host.Mediator.Send(new GetOrderQuery(orderId))).Status
            .Should().Be("payment_verifying");

        // Admin verifies payment.
        await host.Mediator.Send(new VerifyPaymentCommand(payId));
        (await host.Mediator.Send(new GetOrderQuery(orderId))).Status.Should().Be("paid");

        // Kitchen flow.
        await host.Mediator.Send(new AdvanceOrderStatusCommand(orderId, "in_preparation"));
        await host.Mediator.Send(new AdvanceOrderStatusCommand(orderId, "ready"));
        await host.Mediator.Send(new AdvanceOrderStatusCommand(orderId, "delivered"));

        var final = await host.Mediator.Send(new GetOrderQuery(orderId));
        final.Status.Should().Be("delivered");
        final.PaidAt.Should().NotBeNull();
        final.ReadyAt.Should().NotBeNull();
        final.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Rejected_payment_moves_order_to_rejected()
    {
        var adminId = Guid.NewGuid();
        await using var host = new ApplicationTestHost(_fixture, currentUserId: adminId);

        var dishId = await host.Mediator.Send(new CreateDishCommand(
            Name: "Tequeños test", SellingPriceUsd: 4m));

        var orderId = await host.Mediator.Send(new CreateGuestOrderCommand(
            GuestFullName: "Bad Payer",
            GuestPhone: "+58 412-000-0000",
            DeliveryType: "pickup",
            Items: [new OrderLineInput(dishId, 1)]));

        var payId = await host.Mediator.Send(new SubmitPaymentProofCommand(
            OrderId: orderId,
            Method: "transfer_ves",
            AmountUsd: 4m,
            PaidCurrency: "VES",
            AmountPaidCurrency: 160m,
            ExchangeRateUsed: 40m));

        await host.Mediator.Send(new HomeChefPro.Application.Payments.Commands.RejectPayment.RejectPaymentCommand(
            payId, "Comprobante no coincide"));

        var rejected = await host.Mediator.Send(new GetOrderQuery(orderId));
        rejected.Status.Should().Be("rejected");
        rejected.CancellationReason.Should().Be("Comprobante no coincide");
    }
}
