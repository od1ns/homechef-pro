using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Payments.Dtos;
using HomeChefPro.Application.Payments.Mapping;
using HomeChefPro.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Payments.Queries.ListPendingPayments;

public sealed record ListPendingPaymentsQuery : IRequest<IReadOnlyList<PaymentDto>>;

public sealed class ListPendingPaymentsHandler(IHomeChefProDbContext db)
    : IRequestHandler<ListPendingPaymentsQuery, IReadOnlyList<PaymentDto>>
{
    public async Task<IReadOnlyList<PaymentDto>> Handle(
        ListPendingPaymentsQuery request, CancellationToken ct)
    {
        var rows = await db.Payments
            .AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Pending)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(ct).ConfigureAwait(false);
        return rows.Select(p => p.ToDto()).ToArray();
    }
}
