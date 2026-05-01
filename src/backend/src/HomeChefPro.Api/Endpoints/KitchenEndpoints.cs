using HomeChefPro.Application.Orders.Commands.MarkItemReady;
using HomeChefPro.Application.Orders.Commands.StartItemPrep;
using HomeChefPro.Application.Orders.Queries.ListActiveOrders;
using MediatR;

namespace HomeChefPro.Api.Endpoints;

public static class KitchenEndpoints
{
    public static IEndpointRouteBuilder MapKitchenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/kitchen")
            .WithTags("Kitchen")
            .RequireAuthorization("Cook");

        group.MapGet("orders", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListActiveOrdersQuery(), ct)));

        group.MapPost("orders/{orderId:guid}/items/{itemId:guid}/start", async (
            Guid orderId, Guid itemId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new StartItemPrepCommand(orderId, itemId), ct);
            return Results.NoContent();
        });

        group.MapPost("orders/{orderId:guid}/items/{itemId:guid}/ready", async (
            Guid orderId, Guid itemId, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new MarkItemReadyCommand(orderId, itemId), ct);
            return Results.NoContent();
        });

        return app;
    }
}
