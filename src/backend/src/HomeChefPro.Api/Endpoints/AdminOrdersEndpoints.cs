using HomeChefPro.Application.Orders.Commands.AdvanceOrderStatus;
using HomeChefPro.Application.Orders.Queries.GetOrder;
using HomeChefPro.Application.Orders.Queries.ListActiveOrders;
using HomeChefPro.Application.Receipts.Queries.GetOrderReceipt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminOrdersEndpoints
{
    public static IEndpointRouteBuilder MapAdminOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/orders")
            .WithTags("Admin: Orders")
            .RequireAuthorization("Cashier"); // Cashier OR Admin (policy defined in AuthConfiguration)

        group.MapGet("", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? statusFilter = null) =>
            Results.Ok(await mediator.Send(new ListActiveOrdersQuery(statusFilter), ct)));

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetOrderQuery(id), ct)));

        group.MapPost("{id:guid}/advance", async (
            Guid id,
            [FromBody] AdvanceRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new AdvanceOrderStatusCommand(id, body.Target, body.Reason), ct);
            return Results.NoContent();
        });

        group.MapGet("{id:guid}/receipt.pdf", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var pdf = await mediator.Send(new GetOrderReceiptQuery(id), ct);
            return Results.File(pdf.Pdf, pdf.ContentType, pdf.FileName);
        });

        return app;
    }

    public sealed record AdvanceRequest(string Target, string? Reason = null);
}
