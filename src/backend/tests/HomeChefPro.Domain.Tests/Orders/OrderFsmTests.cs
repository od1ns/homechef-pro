using FluentAssertions;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;

namespace HomeChefPro.Domain.Tests.Orders;

public class OrderFsmTests
{
    private static Order NewGuestOrder(DeliveryType delivery = DeliveryType.Pickup, string? address = null)
    {
        var order = Order.CreateForGuest(
            guestCustomerId: Guid.NewGuid(),
            deliveryType: delivery,
            deliveryAddress: address);
        order.AddItem(Guid.NewGuid(), "Pabellón", 10m, 2);
        return order;
    }

    [Fact]
    public void Happy_path_pickup_pending_payment_to_delivered()
    {
        var order = NewGuestOrder();

        order.Status.Should().Be(OrderStatus.PendingPayment);
        order.SubmitPayment();
        order.Status.Should().Be(OrderStatus.PaymentVerifying);
        order.ApprovePayment();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();

        order.StartPreparation();
        order.Status.Should().Be(OrderStatus.InPreparation);
        order.MarkReady();
        order.Status.Should().Be(OrderStatus.Ready);
        order.ReadyAt.Should().NotBeNull();

        order.MarkDelivered();
        order.Status.Should().Be(OrderStatus.Delivered);
        order.DeliveredAt.Should().NotBeNull();
    }

    [Fact]
    public void Happy_path_third_party_includes_in_delivery_step()
    {
        var order = NewGuestOrder(DeliveryType.ThirdParty, "Av. Francisco de Miranda, Chacao");
        order.SubmitPayment();
        order.ApprovePayment();
        order.StartPreparation();
        order.MarkReady();

        order.DispatchForDelivery();
        order.Status.Should().Be(OrderStatus.InDelivery);
        order.MarkDelivered();
        order.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public void Cannot_skip_states()
    {
        var order = NewGuestOrder();
        var act = () => order.StartPreparation(); // still pending_payment
        act.Should().Throw<DomainException>().WithMessage("*cannot transition*");
    }

    [Fact]
    public void Reject_payment_sets_rejected_with_reason()
    {
        var order = NewGuestOrder();
        order.SubmitPayment();
        order.RejectPayment("Comprobante ilegible");

        order.Status.Should().Be(OrderStatus.Rejected);
        order.CancellationReason.Should().Be("Comprobante ilegible");
        order.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_before_delivery_is_allowed()
    {
        var order = NewGuestOrder();
        order.SubmitPayment();
        order.ApprovePayment();
        order.Cancel("Cliente se arrepintió");
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void Cannot_cancel_a_delivered_order()
    {
        var order = NewGuestOrder();
        order.SubmitPayment();
        order.ApprovePayment();
        order.StartPreparation();
        order.MarkReady();
        order.MarkDelivered();
        var act = () => order.Cancel("too late");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cannot_add_items_after_payment_verified()
    {
        var order = NewGuestOrder();
        order.SubmitPayment();
        order.ApprovePayment();
        var act = () => order.AddItem(Guid.NewGuid(), "Reina", 6m, 1);
        act.Should().Throw<DomainException>().WithMessage("*Paid*");
    }

    [Fact]
    public void Totals_recompute_on_item_changes()
    {
        var order = NewGuestOrder();
        order.SubtotalUsd.Should().Be(20m);
        order.AddItem(Guid.NewGuid(), "Cachapa", 5m, 3);
        order.SubtotalUsd.Should().Be(35m);
        order.TotalUsd.Should().Be(35m);
    }

    [Fact]
    public void Exchange_snapshot_computes_ves_total()
    {
        var order = NewGuestOrder();
        order.ApplyExchangeSnapshot(Guid.NewGuid(), 40m);
        // subtotal = 20 USD, rate 40 VES/USD -> 800 VES
        order.TotalVesAtOrderTime.Should().Be(800m);
    }

    [Fact]
    public void Kitchen_item_transitions_follow_order_state()
    {
        var order = NewGuestOrder();
        order.SubmitPayment();
        order.ApprovePayment();
        var itemId = order.Items[0].Id;

        order.StartItemPrep(itemId);
        order.Status.Should().Be(OrderStatus.InPreparation);
        order.Items[0].KitchenStatus.Should().Be(KitchenStatus.InPrep);

        order.MarkItemReady(itemId);
        // Only one item, so order goes to ready automatically
        order.Status.Should().Be(OrderStatus.Ready);
    }

    [Fact]
    public void Third_party_delivery_requires_address()
    {
        var act = () => Order.CreateForGuest(
            guestCustomerId: Guid.NewGuid(),
            deliveryType: DeliveryType.ThirdParty);
        act.Should().Throw<DomainException>().WithMessage("*address*");
    }
}
