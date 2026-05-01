using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Catalog.Ingredients.Dtos;
using HomeChefPro.Application.Catalog.Ingredients.Mapping;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Ingredients.Queries.GetIngredient;

public sealed record GetIngredientQuery(Guid Id) : IRequest<IngredientDto>;

public sealed class GetIngredientHandler(IHomeChefProDbContext db)
    : IRequestHandler<GetIngredientQuery, IngredientDto>
{
    public async Task<IngredientDto> Handle(GetIngredientQuery request, CancellationToken ct)
    {
        var ingredient = await db.Ingredients
            .AsNoTracking()
            .Include(i => i.Presentations)
            .FirstOrDefaultAsync(i => i.Id == request.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Ingredient), request.Id);

        return ingredient.ToDto();
    }
}
