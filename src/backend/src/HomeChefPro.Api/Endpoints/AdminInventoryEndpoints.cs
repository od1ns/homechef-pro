using HomeChefPro.Application.Inventory.Commands.RecordPurchase;
using HomeChefPro.Application.Inventory.Commands.RecordWaste;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminInventoryEndpoints
{
    public static IEndpointRouteBuilder MapAdminInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/inventory")
            .WithTags("Admin: Inventory")
            .RequireAuthorization("Admin");

        group.MapPost("purchases", async (
            [FromBody] RecordPurchaseCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/inventory/purchases/{id}", id);
        });

        group.MapPost("waste", async (
            [FromBody] RecordWasteCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/inventory/waste/{id}", id);
        });

        return app;
    }
}
