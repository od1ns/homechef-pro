using HomeChefPro.Application.Inventory.Queries.ForecastPurchases;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminPurchasingEndpoints
{
    public static IEndpointRouteBuilder MapAdminPurchasingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/purchasing")
            .WithTags("Admin: Purchasing")
            .RequireAuthorization("Admin");

        group.MapGet("forecast", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] int historicalDays = 28,
            [FromQuery] int targetDays = 7,
            [FromQuery] decimal growthFactor = 1.0m) =>
            Results.Ok(await mediator.Send(new ForecastPurchasesQuery(
                historicalDays, targetDays, growthFactor), ct)));

        return app;
    }
}
