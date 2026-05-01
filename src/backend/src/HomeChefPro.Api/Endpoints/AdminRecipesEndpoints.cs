using HomeChefPro.Application.Catalog.Recipes.Commands.AddIngredientComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.AddSubRecipeComponent;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateSubRecipe;
using HomeChefPro.Application.Catalog.Recipes.Commands.ToggleOutOfStock;
using HomeChefPro.Application.Catalog.Recipes.Commands.UpdateSellingPrice;
using HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipe;
using HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipeCost;
using HomeChefPro.Application.Catalog.Recipes.Queries.ListRecipes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class AdminRecipesEndpoints
{
    public static IEndpointRouteBuilder MapAdminRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/recipes")
            .WithTags("Admin: Recipes")
            .RequireAuthorization("Admin");

        group.MapGet("", async (
            IMediator mediator,
            CancellationToken ct,
            [FromQuery] bool includeSubRecipes = false,
            [FromQuery] bool onlyActive = true,
            [FromQuery] bool onlyOnMenu = false,
            [FromQuery] string? menuType = null,
            [FromQuery] string? search = null) =>
        {
            var list = await mediator.Send(new ListRecipesQuery(
                includeSubRecipes, onlyActive, onlyOnMenu, menuType, search), ct);
            return Results.Ok(list);
        });

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetRecipeQuery(id), ct)));

        group.MapGet("{id:guid}/cost", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetRecipeCostQuery(id), ct)));

        group.MapPost("dishes", async (
            [FromBody] CreateDishCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/recipes/{id}", id);
        });

        group.MapPost("sub-recipes", async (
            [FromBody] CreateSubRecipeCommand cmd, IMediator mediator, CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/admin/recipes/{id}", id);
        });

        group.MapPost("{id:guid}/components/ingredient", async (
            Guid id,
            [FromBody] AddIngredientComponentRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var componentId = await mediator.Send(new AddIngredientComponentCommand(
                RecipeId: id,
                IngredientId: body.IngredientId,
                Quantity: body.Quantity,
                Notes: body.Notes,
                DisplayOrder: body.DisplayOrder), ct);
            return EndpointResults.CreatedId(
                $"/api/admin/recipes/{id}/components/{componentId}", componentId);
        });

        group.MapPost("{id:guid}/components/sub-recipe", async (
            Guid id,
            [FromBody] AddSubRecipeComponentRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var componentId = await mediator.Send(new AddSubRecipeComponentCommand(
                RecipeId: id,
                SubRecipeId: body.SubRecipeId,
                Quantity: body.Quantity,
                Notes: body.Notes,
                DisplayOrder: body.DisplayOrder), ct);
            return EndpointResults.CreatedId(
                $"/api/admin/recipes/{id}/components/{componentId}", componentId);
        });

        group.MapPatch("{id:guid}/selling-price", async (
            Guid id,
            [FromBody] UpdateSellingPriceRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new UpdateSellingPriceCommand(id, body.SellingPriceUsd), ct);
            return Results.NoContent();
        });

        group.MapPost("{id:guid}/out-of-stock", async (
            Guid id,
            [FromBody] ToggleOutOfStockRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            await mediator.Send(new ToggleOutOfStockCommand(id, body.OutOfStock), ct);
            return Results.NoContent();
        });

        return app;
    }

    public sealed record AddIngredientComponentRequest(
        Guid IngredientId, decimal Quantity, string? Notes = null, int DisplayOrder = 0);

    public sealed record AddSubRecipeComponentRequest(
        Guid SubRecipeId, decimal Quantity, string? Notes = null, int DisplayOrder = 0);

    public sealed record UpdateSellingPriceRequest(decimal SellingPriceUsd);
    public sealed record ToggleOutOfStockRequest(bool OutOfStock);
}
