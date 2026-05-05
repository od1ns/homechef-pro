using HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipe;
using HomeChefPro.Application.Catalog.Recipes.Queries.ListRecipes;
using MediatR;

namespace HomeChefPro.Api.Endpoints;

public static class ClientMenuEndpoints
{
    public static IEndpointRouteBuilder MapClientMenuEndpoints(this IEndpointRouteBuilder app)
    {
        // F-28 (Tier 2): rate limiting "public" (30 req/min/IP) — el menu lo
        // consume el cliente anonimo cada vez que abre la app.
        var group = app.MapGroup("/api/client/menu")
            .WithTags("Client: Menu")
            .AllowAnonymous()
            .RequireRateLimiting("public");

        group.MapGet("", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListRecipesQuery(
                IncludeSubRecipes: false,
                OnlyActive: true,
                OnlyOnMenu: true), ct)));

        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
        {
            var recipe = await mediator.Send(new GetRecipeQuery(id), ct);
            if (recipe.IsSubRecipe || !recipe.IsActive)
                return Results.NotFound();
            return Results.Ok(recipe);
        });

        return app;
    }
}
