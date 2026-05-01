using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.AddIngredientComponent;

public sealed record AddIngredientComponentCommand(
    Guid RecipeId,
    Guid IngredientId,
    decimal Quantity,
    string? Notes = null,
    int DisplayOrder = 0) : IRequest<Guid>;

public sealed class AddIngredientComponentValidator : AbstractValidator<AddIngredientComponentCommand>
{
    public AddIngredientComponentValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(200);
    }
}

public sealed class AddIngredientComponentHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<AddIngredientComponentCommand, Guid>
{
    public async Task<Guid> Handle(AddIngredientComponentCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .Include(r => r.Components)
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        var exists = await db.Ingredients.AnyAsync(i => i.Id == request.IngredientId, ct)
            .ConfigureAwait(false);
        if (!exists)
            throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        var component = recipe.AddIngredient(
            ingredientId: request.IngredientId,
            quantity: request.Quantity,
            notes: request.Notes,
            displayOrder: request.DisplayOrder,
            clock: clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return component.Id;
    }
}
