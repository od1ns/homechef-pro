using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.CreateRecipeModifier;

public sealed record CreateRecipeModifierCommand(
    Guid RecipeId,
    string Name,
    int DefaultQty = 0,
    int MinQty = 0,
    int MaxQty = 1,
    decimal PriceDeltaUsd = 0m,
    int DisplayOrder = 0) : IRequest<Guid>;

public sealed class CreateRecipeModifierValidator : AbstractValidator<CreateRecipeModifierCommand>
{
    public CreateRecipeModifierValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
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

public sealed class CreateRecipeModifierHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<CreateRecipeModifierCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecipeModifierCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .Include(r => r.Modifiers)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        if (recipe.IsSubRecipe)
            throw new InvalidOperationException("Las sub-recetas no pueden tener modificadores.");

        var modifier = recipe.AddModifier(
            name: request.Name,
            defaultQty: request.DefaultQty,
            minQty: request.MinQty,
            maxQty: request.MaxQty,
            priceDeltaUsd: request.PriceDeltaUsd,
            displayOrder: request.DisplayOrder,
            clock: clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return modifier.Id;
    }
}
