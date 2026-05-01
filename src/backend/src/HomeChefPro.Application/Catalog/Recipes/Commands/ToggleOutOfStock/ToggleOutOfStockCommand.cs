using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.ToggleOutOfStock;

public sealed record ToggleOutOfStockCommand(Guid RecipeId, bool OutOfStock) : IRequest;

public sealed class ToggleOutOfStockHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<ToggleOutOfStockCommand>
{
    public async Task Handle(ToggleOutOfStockCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes.FindAsync([request.RecipeId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        if (request.OutOfStock) recipe.MarkOutOfStock(clock);
        else recipe.MarkBackInStock(clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
