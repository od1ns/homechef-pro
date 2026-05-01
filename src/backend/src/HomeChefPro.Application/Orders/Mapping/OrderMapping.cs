using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;

namespace HomeChefPro.Application.Orders.Mapping;

public static class OrderMapping
{
    public static OrderDto ToDto(this Order o) =>
        new(
            Id: o.Id,
            OrderNumber: o.OrderNumber,
            CustomerType: EnumDbMap<CustomerType>.ToDb(o.CustomerType),
            UserId: o.UserId,
            GuestCustomerId: o.GuestCustomerId,
            Status: EnumDbMap<OrderStatus>.ToDb(o.Status),
            DeliveryType: EnumDbMap<DeliveryType>.ToDb(o.DeliveryType),
            DeliveryAddress: o.DeliveryAddress,
            DeliveryInstructions: o.DeliveryInstructions,
            ContactPhone: o.ContactPhone,
            ScheduledFor: o.ScheduledFor,
            PrepEstimatedReadyAt: o.PrepEstimatedReadyAt,
            CustomerNotes: o.CustomerNotes,
            SubtotalUsd: o.SubtotalUsd,
            DiscountUsd: o.DiscountUsd,
            DeliveryFeeUsd: o.DeliveryFeeUsd,
            TotalUsd: o.TotalUsd,
            RateVesPerUsdAtOrder: o.RateVesPerUsdAtOrder,
            TotalVesAtOrderTime: o.TotalVesAtOrderTime,
            CreatedAt: o.CreatedAt,
            UpdatedAt: o.UpdatedAt,
            PaidAt: o.PaidAt,
            PrepStartedAt: o.PrepStartedAt,
            ReadyAt: o.ReadyAt,
            DeliveredAt: o.DeliveredAt,
            CancelledAt: o.CancelledAt,
            CancellationReason: o.CancellationReason,
            Items: o.Items.Select(ToDto).ToArray());

    public static OrderItemDto ToDto(this OrderItem i) =>
        new(
            Id: i.Id,
            DishId: i.DishId,
            DishNameSnapshot: i.DishNameSnapshot,
            UnitPriceUsd: i.UnitPriceUsd,
            Quantity: i.Quantity,
            LineTotalUsd: i.LineTotalUsd,
            ItemNotes: i.ItemNotes,
            KitchenStatus: EnumDbMap<KitchenStatus>.ToDb(i.KitchenStatus),
            PrepStartedAt: i.PrepStartedAt,
            PrepCompletedAt: i.PrepCompletedAt);
}
