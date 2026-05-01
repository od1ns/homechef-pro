using HomeChefPro.Application.Invoicing.Commands.CancelInvoice;
using HomeChefPro.Application.Invoicing.Commands.EmitInvoice;
using HomeChefPro.Application.Invoicing.Queries.GetInvoice;
using HomeChefPro.Application.Invoicing.Queries.ListInvoices;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminInvoicesEndpoints
{
    public static IEndpointRouteBuilder MapAdminInvoicesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/invoices")
            .WithTags("Admin: Invoices")
            .RequireAuthorization("Admin");

        group.MapGet("", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? statusFilter = null,
            [FromQuery] int days = 90) =>
            Results.Ok(await mediator.Send(new ListInvoicesQuery(statusFilter, days), ct)));

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetInvoiceQuery(id), ct)));

        group.MapPost("", async (
            [FromBody] EmitInvoiceCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var dto = await mediator.Send(cmd, ct);
            return Results.Created($"/api/admin/invoices/{dto.Id}", dto);
        });

        group.MapPost("{id:guid}/cancel", async (
            Guid id,
            [FromBody] CancelBody body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new CancelInvoiceCommand(id, body.Reason), ct);
            return Results.NoContent();
        });

        return app;
    }

    public sealed record CancelBody(string Reason);
}
