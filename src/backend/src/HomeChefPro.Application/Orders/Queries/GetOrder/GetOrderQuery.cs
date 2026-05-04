using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Orders.Mapping;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Queries.GetOrder;

/// <summary>
/// Lookup de un order por ID. Si <paramref name="AccessToken"/> es no-null, el handler
/// valida que coincida con <c>order.AccessToken</c> (anti-IDOR para clientes anonymous).
/// Si es null, asume que el caller ya pasó por <c>RequireAuthorization</c> (admin/cashier).
/// F-24 (audit Pasada B).
/// </summary>
public sealed record GetOrderQuery(Guid Id, string? AccessToken = null) : IRequest<OrderDto>;

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

        // F-24: si el caller envia token (cliente anonymous), validar match constante.
        // Si no envia (admin/cashier ya autenticado por policy), permitir.
        // En caso de mismatch devolvemos NotFoundException → 404 (no 401) para no
        // revelar la existencia del order a quien adivina IDs.
        if (request.AccessToken is not null
            && !string.Equals(order.AccessToken, request.AccessToken, StringComparison.Ordinal))
        {
            throw new NotFoundException(nameof(Order), request.Id);
        }

        return order.ToDto();
    }
}
