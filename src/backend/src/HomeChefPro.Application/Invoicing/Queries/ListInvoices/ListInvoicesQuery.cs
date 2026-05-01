using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Application.Invoicing.Mapping;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Invoicing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Invoicing.Queries.ListInvoices;

public sealed record ListInvoicesQuery(
    string? StatusFilter = null,
    int Days = 90) : IRequest<IReadOnlyList<InvoiceDto>>;

public sealed class ListInvoicesHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<ListInvoicesQuery, IReadOnlyList<InvoiceDto>>
{
    public async Task<IReadOnlyList<InvoiceDto>> Handle(
        ListInvoicesQuery request, CancellationToken ct)
    {
        var since = clock.GetUtcNow().AddDays(-Math.Clamp(request.Days, 1, 365));
        var query = db.Invoices.AsNoTracking()
            .Where(i => i.CreatedAt >= since);

        if (!string.IsNullOrWhiteSpace(request.StatusFilter)
            && EnumDbMap<InvoiceStatus>.TryFromDb(request.StatusFilter, out var parsed))
        {
            query = query.Where(i => i.Status == parsed);
        }

        var rows = await query
            .OrderByDescending(i => i.CreatedAt)
            .Join(db.Orders.AsNoTracking(),
                i => i.OrderId,
                o => o.Id,
                (i, o) => new { Invoice = i, OrderNumber = o.OrderNumber })
            .ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(r => r.Invoice.ToDto(r.OrderNumber)).ToArray();
    }
}
