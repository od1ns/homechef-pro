using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Queries.ListScheduledOrders;

/// <summary>
/// Etapa 4: pedidos con entrega programada (scheduled_for IS NOT NULL) que aun no
/// han terminado. Ordenados por scheduled_for ASC para que la cocina vea primero
/// el mas urgente.
/// </summary>
public sealed record ListScheduledOrdersQuery : IRequest<IReadOnlyList<OrderSummaryDto>>;

public sealed class ListScheduledOrdersHandler(IHomeChefProDbContext db)
    : IRequestHandler<ListScheduledOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    private static readonly OrderStatus[] TerminalStatuses =
    [
        OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.Rejected,
    ];

    public async Task<IReadOnlyList<OrderSummaryDto>> Handle(
        ListScheduledOrdersQuery request, CancellationToken ct)
    {
        var rows = await db.Orders.AsNoTracking()
            .Where(o => o.ScheduledFor != null && !TerminalStatuses.Contains(o.Status))
            .OrderBy(o => o.ScheduledFor)
            .Select(o => new
            {
                o.Id,
                o.OrderNumber,
                o.Status,
                o.DeliveryType,
                o.TotalUsd,
                UserProfileName = db.UserProfiles
                    .Where(p => p.Id == o.UserId)
                    .Select(p => p.FullName)
                    .FirstOrDefault(),
                GuestName = db.GuestCustomers
                    .Where(g => g.Id == o.GuestCustomerId)
                    .Select(g => g.FullName)
                    .FirstOrDefault(),
                ItemCount = o.Items.Count,
                o.CreatedAt,
                o.PrepEstimatedReadyAt,
                o.ScheduledFor,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(r => new OrderSummaryDto(
            Id: r.Id,
            OrderNumber: r.OrderNumber,
            Status: EnumDbMap<OrderStatus>.ToDb(r.Status),
            DeliveryType: EnumDbMap<DeliveryType>.ToDb(r.DeliveryType),
            TotalUsd: r.TotalUsd,
            CustomerName: r.UserProfileName ?? r.GuestName ?? "Unknown",
            ItemCount: r.ItemCount,
            CreatedAt: r.CreatedAt,
            PrepEstimatedReadyAt: r.PrepEstimatedReadyAt,
            ScheduledFor: r.ScheduledFor)).ToArray();
    }
}
