using HomeChefPro.Application.Reviews.Commands.EditReview;
using HomeChefPro.Application.Reviews.Commands.LeaveReview;
using HomeChefPro.Application.Reviews.Queries.ListMyReviews;
using HomeChefPro.Application.Reviews.Queries.ListReviewsForDish;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class ClientReviewsEndpoints
{
    public static IEndpointRouteBuilder MapClientReviewsEndpoints(this IEndpointRouteBuilder app)
    {
        // Public: anyone can see visible reviews for a dish.
        app.MapGet("/api/client/menu/{dishId:guid}/reviews", async (
            Guid dishId, IMediator mediator, CancellationToken ct,
            [FromQuery] int take = 50) =>
            Results.Ok(await mediator.Send(new ListReviewsForDishQuery(dishId, take), ct)))
        .WithTags("Client: Reviews")
        .AllowAnonymous();

        // Authenticated customer endpoints.
        var authed = app.MapGroup("/api/client/reviews")
            .WithTags("Client: Reviews")
            .RequireAuthorization();

        authed.MapPost("", async (
            [FromBody] LeaveReviewCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/client/reviews/{id}", id);
        });

        authed.MapPatch("{id:guid}", async (
            Guid id,
            [FromBody] EditReviewBody body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new EditReviewCommand(id, body.Rating, body.Comment), ct);
            return Results.NoContent();
        });

        authed.MapGet("mine", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListMyReviewsQuery(), ct)));

        return app;
    }

    public sealed record EditReviewBody(short Rating, string? Comment = null);
}
