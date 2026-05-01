using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Application.Invoicing.Mapping;
using HomeChefPro.Domain.Invoicing;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Invoicing.Queries.GetInvoice;

public sealed record GetInvoiceQuery(Guid InvoiceId) : IRequest<InvoiceDto>;

public sealed class GetInvoiceHandler(IHomeChefProDbContext db)
    : IRequestHandler<GetInvoiceQuery, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(GetInvoiceQuery request, CancellationToken ct)
    {
        var row = await db.Invoices.AsNoTracking()
            .Where(i => i.Id == request.InvoiceId)
            .Join(db.Orders.AsNoTracking(),
                i => i.OrderId, o => o.Id,
                (i, o) => new { Invoice = i, OrderNumber = o.OrderNumber })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Invoice), request.InvoiceId);

        return row.Invoice.ToDto(row.OrderNumber);
    }
}
