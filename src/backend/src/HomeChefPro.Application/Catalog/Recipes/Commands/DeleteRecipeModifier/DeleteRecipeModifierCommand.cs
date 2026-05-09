using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.DeleteRecipeModifier;

/// <summary>
/// Soft-delete: marca el modificador como inactivo (is_active = false).
/// Los snapshots en order_item_modifiers siguen intactos (FK a modifier_id).
/// </summary>
public sealed record DeleteRecipeModifierCommand(Guid RecipeId, Guid ModifierId) : IRequest;

public sealed class DeleteRecipeModifierHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<DeleteRecipeModifierCommand>
{
    public async Task Handle(DeleteRecipeModifierCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .Include(r => r.Modifiers)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        recipe.RemoveModifier(request.ModifierId, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
