using HomeChefPro.Application.Catalog.Ingredients.Commands.AddPresentation;
using HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;
using HomeChefPro.Application.Catalog.Ingredients.Commands.DeactivateIngredient;
using HomeChefPro.Application.Catalog.Ingredients.Commands.UpdateReorderThresholds;
using HomeChefPro.Application.Catalog.Ingredients.Queries.GetIngredient;
using HomeChefPro.Application.Catalog.Ingredients.Queries.ListIngredients;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminIngredientsEndpoints
{
    public static IEndpointRouteBuilder MapAdminIngredientsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/ingredients")
            .WithTags("Admin: Ingredients")
            .RequireAuthorization("Admin");

        group.MapGet("", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool onlyActive = true,
            [FromQuery] bool onlyBelowReorder = false,
            [FromQuery] string? search = null) =>
        {
            var list = await mediator.Send(new ListIngredientsQuery(onlyActive, onlyBelowReorder, search), ct);
            return Results.Ok(list);
        });

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var dto = await mediator.Send(new GetIngredientQuery(id), ct);
            return Results.Ok(dto);
        });

        group.MapPost("", async (
            [FromBody] CreateIngredientCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/ingredients/{id}", id);
        });

        group.MapPost("{id:guid}/presentations", async (
            Guid id,
            [FromBody] AddPresentationRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = new AddPresentationCommand(
                IngredientId: id,
                Name: body.Name,
                PurchaseUnit: body.PurchaseUnit,
                PurchaseQuantity: body.PurchaseQuantity,
                ConversionToUseUnit: body.ConversionToUseUnit,
                LastPurchasePriceUsd: body.LastPurchasePriceUsd);
            var presId = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId(
                $"/api/admin/ingredients/{id}/presentations/{presId}", presId);
        });

        group.MapPatch("{id:guid}/thresholds", async (
            Guid id,
            [FromBody] UpdateThresholdsRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new UpdateReorderThresholdsCommand(
                IngredientId: id,
                ReorderPointUseUnit: body.ReorderPointUseUnit,
                MinimumStockUseUnit: body.MinimumStockUseUnit), ct);
            return Results.NoContent();
        });

        group.MapPost("{id:guid}/deactivate", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
            await mediator.Send(new DeactivateIngredientCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }

    public sealed record AddPresentationRequest(
        string Name,
        string PurchaseUnit,
        decimal PurchaseQuantity,
        decimal ConversionToUseUnit,
        decimal? LastPurchasePriceUsd = null);

    public sealed record UpdateThresholdsRequest(
        decimal ReorderPointUseUnit,
        decimal MinimumStockUseUnit);
}
