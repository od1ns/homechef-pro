using HomeChefPro.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminReportsEndpoints
{
    public static IEndpointRouteBuilder MapAdminReportsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/reports")
            .WithTags("Admin: Reports")
            .RequireAuthorization("Admin");

        group.MapGet("dish-margin", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new DishProfitMarginQuery(), ct)));

        group.MapGet("recipe-costs", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeSubRecipes = false) =>
            Results.Ok(await mediator.Send(new RecipeFullCostsQuery(includeSubRecipes), ct)));

        group.MapGet("reorder-suggestions", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? priority = null) =>
            Results.Ok(await mediator.Send(new ReorderSuggestionsQuery(priority), ct)));

        group.MapGet("sales-daily", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] int days = 30) =>
            Results.Ok(await mediator.Send(new SalesDailyQuery(days), ct)));

        // Rotacion de inventario por ingrediente. Categorias:
        //   alta (>12 vueltas/año, < 30 dias de stock)
        //   media (4-12 vueltas/año, 30-90 dias)
        //   baja (<4 vueltas/año, >90 dias - capital muerto)
        //   inactivo (sin compras ni consumo en 60 dias)
        // Filtro opcional ?category=alta|media|baja|inactivo
        group.MapGet("inventory-rotation", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? category = null) =>
            Results.Ok(await mediator.Send(new InventoryRotationQuery(category), ct)));

        return app;
    }
}
