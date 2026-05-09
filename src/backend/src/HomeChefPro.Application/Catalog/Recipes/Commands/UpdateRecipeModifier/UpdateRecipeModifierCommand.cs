using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.UpdateRecipeModifier;

public sealed record UpdateRecipeModifierCommand(
    Guid RecipeId,
    Guid ModifierId,
    string Name,
    int DefaultQty,
    int MinQty,
    int MaxQty,
    decimal PriceDeltaUsd,
    int DisplayOrder) : IRequest;

public sealed class UpdateRecipeModifierValidator : AbstractValidator<UpdateRecipeModifierCommand>
{
    public UpdateRecipeModifierValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.ModifierId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.MinQty).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxQty).GreaterThanOrEqualTo(x => x.MinQty)
            .WithMessage("max_qty debe ser >= min_qty.");
        RuleFor(x => x.DefaultQty)
            .GreaterThanOrEqualTo(x => x.MinQty)
            .LessThanOrEqualTo(x => x.MaxQty)
            .WithMessage("default_qty debe estar en el rango [min_qty, max_qty].");
    }
}

public sealed class UpdateRecipeModifierHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateRecipeModifierCommand>
{
    public async Task Handle(UpdateRecipeModifierCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .Include(r => r.Modifiers)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        recipe.UpdateModifier(
            modifierId: request.ModifierId,
            name: request.Name,
            defaultQty: request.DefaultQty,
            minQty: request.MinQty,
            maxQty: request.MaxQty,
            priceDeltaUsd: request.PriceDeltaUsd,
            displayOrder: request.DisplayOrder,
            clock: clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
