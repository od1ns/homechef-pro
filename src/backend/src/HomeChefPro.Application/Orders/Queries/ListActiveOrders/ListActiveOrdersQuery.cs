using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Queries.ListActiveOrders;

/// <summary>
/// The "live orders" board the admin and kitchen tablets watch.
/// Returns every non-terminal order ordered by creation time ASC (oldest first).
/// </summary>
public sealed record ListActiveOrdersQuery(
    string? StatusFilter = null // optional exact status filter
) : IRequest<IReadOnlyList<OrderSummaryDto>>;

public sealed class ListActiveOrdersHandler(IHomeChefProDbContext db)
    : IRequestHandler<ListActiveOrdersQuery, IReadOnlyList<OrderSummaryDto>>
{
    private static readonly OrderStatus[] TerminalStatuses =
    [
        OrderStatus.Delivered, OrderStatus.Cancelled, OrderStatus.Rejected,
    ];

    public async Task<IReadOnlyList<OrderSummaryDto>> Handle(
        ListActiveOrdersQuery request, CancellationToken ct)
    {
        var query = db.Orders.AsNoTracking()
            .Where(o => !TerminalStatuses.Contains(o.Status))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.StatusFilter)
            && EnumDbMap<OrderStatus>.TryFromDb(request.StatusFilter, out var parsed))
        {
            query = query.Where(o => o.Status == parsed);
        }

        var rows = await query
            .OrderBy(o => o.CreatedAt)
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
            PrepEstimatedReadyAt: r.PrepEstimatedReadyAt)).ToArray();
    }
}
