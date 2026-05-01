using HomeChefPro.Application.Customers.Commands.PutMyPreferences;
using HomeChefPro.Application.Customers.Queries.GetMyPreferences;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class ClientPreferencesEndpoints
{
    public static IEndpointRouteBuilder MapClientPreferencesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/client/me/preferences")
            .WithTags("Client: Preferences")
            .RequireAuthorization();

        group.MapGet("", async (IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetMyPreferencesQuery(), ct);
            // The payload is JSONB; return it as a passthrough object instead of
            // a string-quoted blob so clients can deserialize directly.
            using var doc = System.Text.Json.JsonDocument.Parse(dto.PayloadJson);
            return Results.Ok(new
            {
                payload = doc.RootElement,
                updatedAt = dto.UpdatedAt,
            });
        });

        group.MapPut("", async (
            [FromBody] System.Text.Json.JsonElement body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var raw = body.GetRawText();
            await mediator.Send(new PutMyPreferencesCommand(raw), ct);
            return Results.NoContent();
        });

        return app;
    }
}
