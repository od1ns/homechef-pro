using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Orders.Mapping;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Queries.GetOrder;

public sealed record GetOrderQuery(Guid Id) : IRequest<OrderDto>;

public sealed class GetOrderHandler(IHomeChefProDbContext db)
    : IRequestHandler<GetOrderQuery, OrderDto>
{
    public async Task<OrderDto> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.Id);

        return order.ToDto();
    }
}
