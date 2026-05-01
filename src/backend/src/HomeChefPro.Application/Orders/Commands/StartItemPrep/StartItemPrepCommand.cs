using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Commands.StartItemPrep;

public sealed record StartItemPrepCommand(Guid OrderId, Guid ItemId) : IRequest;

public sealed class StartItemPrepHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<StartItemPrepCommand>
{
    public async Task Handle(StartItemPrepCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        order.StartItemPrep(request.ItemId, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
