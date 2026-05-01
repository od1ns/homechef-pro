using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Catalog.Ingredients.Dtos;
using HomeChefPro.Application.Catalog.Ingredients.Mapping;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Ingredients.Queries.ListIngredients;

public sealed record ListIngredientsQuery(
    bool OnlyActive = true,
    bool OnlyBelowReorder = false,
    string? Search = null) : IRequest<IReadOnlyList<IngredientDto>>;

public sealed class ListIngredientsHandler(IHomeChefProDbContext db)
    : IRequestHandler<ListIngredientsQuery, IReadOnlyList<IngredientDto>>
{
    public async Task<IReadOnlyList<IngredientDto>> Handle(
        ListIngredientsQuery request, CancellationToken ct)
    {
        var query = db.Ingredients
            .AsNoTracking()
            .Include(i => i.Presentations)
            .AsQueryable();

        if (request.OnlyActive)
            query = query.Where(i => i.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(i => i.Name.ToLower().Contains(term));
        }

        var list = await query.OrderBy(i => i.Name).ToListAsync(ct).ConfigureAwait(false);

        if (request.OnlyBelowReorder)
            list = list.Where(i => i.IsBelowReorderPoint).ToList();

        return list.Select(i => i.ToDto()).ToArray();
    }
}
