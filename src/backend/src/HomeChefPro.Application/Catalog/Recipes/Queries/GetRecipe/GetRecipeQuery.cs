using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Catalog.Recipes.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Mapping;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Queries.GetRecipe;

public sealed record GetRecipeQuery(Guid Id) : IRequest<RecipeDto>;

public sealed class GetRecipeHandler(IHomeChefProDbContext db)
    : IRequestHandler<GetRecipeQuery, RecipeDto>
{
    public async Task<RecipeDto> Handle(GetRecipeQuery request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Components)
            .Include(r => r.Modifiers)  // Etapa 2
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.Id);

        return recipe.ToDto();
    }
}
