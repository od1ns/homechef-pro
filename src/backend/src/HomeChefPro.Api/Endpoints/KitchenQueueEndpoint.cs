using HomeChefPro.Application.Reports.Queries;
using MediatR;

namespace HomeChefPro.Api.Endpoints;

public static class KitchenQueueEndpoint
{
    /// <summary>
    /// Rich kitchen feed (joined with recipes' procedure_markdown + prep time + priority_time)
    /// for the tablet UI. Lives at /api/kitchen/queue alongside the existing endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapKitchenQueueEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/kitchen/queue", async (IMediator mediator, CancellationToken ct) =>
                Results.Ok(await mediator.Send(new KitchenQueueQuery(), ct)))
            .WithTags("Kitchen")
            .RequireAuthorization("Cook");

        return app;
    }
}
