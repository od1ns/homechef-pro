using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Catalog.Recipes.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Mapping;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Catalog.Recipes.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipeCost;

public sealed record GetRecipeCostQuery(Guid RecipeId) : IRequest<RecipeCostDto>;

public sealed class GetRecipeCostHandler(IHomeChefProDbContext db)
    : IRequestHandler<GetRecipeCostQuery, RecipeCostDto>
{
    public async Task<RecipeCostDto> Handle(GetRecipeCostQuery request, CancellationToken ct)
    {
        // Exists check
        var exists = await db.Recipes.AnyAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(Recipe), request.RecipeId);

        // Load the full graph: all recipes with components + all ingredients.
        // For a single-kitchen setup with a handful of recipes, loading everything is cheap and
        // avoids N+1 traversals during the recursive cost walk.
        var recipes = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Components)
            .ToListAsync(ct).ConfigureAwait(false);

        var ingredients = await db.Ingredients
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

        var calculator = new RecipeCostCalculator(ingredients, recipes);
        var breakdown = calculator.Calculate(request.RecipeId);
        return breakdown.ToDto();
    }
}
