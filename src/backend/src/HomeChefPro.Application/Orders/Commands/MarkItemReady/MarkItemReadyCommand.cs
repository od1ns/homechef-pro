using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Commands.MarkItemReady;

public sealed record MarkItemReadyCommand(Guid OrderId, Guid ItemId) : IRequest;

public sealed class MarkItemReadyHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<MarkItemReadyCommand>
{
    public async Task Handle(MarkItemReadyCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.MarkItemReady(request.ItemId, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
