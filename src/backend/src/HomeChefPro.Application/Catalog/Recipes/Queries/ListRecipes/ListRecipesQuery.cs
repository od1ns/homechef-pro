using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Catalog.Recipes.Dtos;
using HomeChefPro.Application.Catalog.Recipes.Mapping;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Queries.ListRecipes;

public sealed record ListRecipesQuery(
    bool IncludeSubRecipes = false,
    bool OnlyActive = true,
    bool OnlyOnMenu = false,
    string? MenuType = null,
    string? Search = null) : IRequest<IReadOnlyList<RecipeSummaryDto>>;

public sealed class ListRecipesHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<ListRecipesQuery, IReadOnlyList<RecipeSummaryDto>>
{
    public async Task<IReadOnlyList<RecipeSummaryDto>> Handle(
        ListRecipesQuery request, CancellationToken ct)
    {
        var query = db.Recipes.AsNoTracking().AsQueryable();

        if (!request.IncludeSubRecipes) query = query.Where(r => !r.IsSubRecipe);
        if (request.OnlyActive) query = query.Where(r => r.IsActive);

        if (!string.IsNullOrWhiteSpace(request.MenuType))
        {
            if (!EnumDbMap<MenuType>.TryFromDb(request.MenuType, out var mt))
                return [];
            query = query.Where(r => r.MenuType == mt);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(r => r.Name.ToLower().Contains(term));
        }

        var list = await query.OrderBy(r => r.IsSubRecipe).ThenBy(r => r.Name)
            .ToListAsync(ct).ConfigureAwait(false);

        if (request.OnlyOnMenu)
        {
            var now = clock.GetUtcNow();
            list = list.Where(r => r.IsOnMenuAt(now)).ToList();
        }

        return list.Select(r => r.ToSummary()).ToArray();
    }
}
