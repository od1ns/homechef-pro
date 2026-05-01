using HomeChefPro.Application.Payments.Commands.RejectPayment;
using HomeChefPro.Application.Payments.Commands.VerifyPayment;
using HomeChefPro.Application.Payments.Queries.ListPendingPayments;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminPaymentsEndpoints
{
    public static IEndpointRouteBuilder MapAdminPaymentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/payments")
            .WithTags("Admin: Payments")
            .RequireAuthorization("Cashier");

        group.MapGet("pending", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListPendingPaymentsQuery(), ct)));

        group.MapPost("{id:guid}/verify", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new VerifyPaymentCommand(id), ct);
            return Results.NoContent();
        });

        group.MapPost("{id:guid}/reject", async (
            Guid id,
            [FromBody] RejectRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new RejectPaymentCommand(id, body.Reason), ct);
            return Results.NoContent();
        });

        return app;
    }

    public sealed record RejectRequest(string Reason);
}
