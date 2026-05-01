using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using MediatR;

namespace HomeChefPro.Application.Catalog.Ingredients.Commands.DeactivateIngredient;

public sealed record DeactivateIngredientCommand(Guid IngredientId) : IRequest;

public sealed class DeactivateIngredientHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<DeactivateIngredientCommand>
{
    public async Task Handle(DeactivateIngredientCommand request, CancellationToken ct)
    {
        var ingredient = await db.Ingredients.FindAsync([request.IngredientId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        ingredient.Deactivate(clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
