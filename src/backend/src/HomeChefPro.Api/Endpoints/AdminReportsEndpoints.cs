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

        // Ranking de clientes (analisis RFM: Recency/Frequency/Monetary).
        // Devuelve hasta `limit` clientes ordenados por segmento (vip primero,
        // dormido al final) y dentro de cada segmento por gasto lifetime.
        // Filtro opcional ?segment=vip|regular|casual|dormido
        group.MapGet("customer-ranking", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] string? segment = null,
            [FromQuery] int limit = 50) =>
            Results.Ok(await mediator.Send(new CustomerRankingQuery(segment, limit), ct)));

        // Heatmap de demanda por dia-de-la-semana x hora-del-dia (zona Caracas).
        // Devuelve hasta 7*24 = 168 celdas con orders_count y revenue por celda.
        // Day of week: 0=domingo, 6=sabado.
        group.MapGet("peak-hours-heatmap", async (
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(new PeakHoursHeatmapQuery(), ct)));

        // Resumen: hora pico por cada dia de la semana (la hora con mas
        // orders en cada dia de la semana).
        group.MapGet("peak-hours-summary", async (
            IMediator mediator,
            CancellationToken ct) =>
            Results.Ok(await mediator.Send(new PeakHoursSummaryQuery(), ct)));

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
