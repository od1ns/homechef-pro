using HomeChefPro.Application.Reviews.Commands.ModerateReview;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminReviewsEndpoints
{
    public static IEndpointRouteBuilder MapAdminReviewsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/reviews")
            .WithTags("Admin: Reviews")
            .RequireAuthorization("Admin");

        group.MapPost("{id:guid}/hide", async (
            Guid id,
            [FromBody] ModerationNote? body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new HideReviewCommand(id, body?.Note), ct);
            return Results.NoContent();
        });

        group.MapPost("{id:guid}/restore", async (
            Guid id,
            [FromBody] ModerationNote? body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new RestoreReviewCommand(id, body?.Note), ct);
            return Results.NoContent();
        });

        return app;
    }

    public sealed record ModerationNote(string? Note);
}
