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
        var recipeExists = await db.Recipes
            .AsNoTracking()
            .AnyAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false);
        if (!recipeExists)
            throw new NotFoundException(nameof(Recipe), request.RecipeId);

        var ingredientExists = await db.Ingredients
            .AsNoTracking()
            .AnyAsync(i => i.Id == request.IngredientId, ct)
            .ConfigureAwait(false);
        if (!ingredientExists)
            throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        var dup = await db.RecipeComponents
            .AsNoTracking()
            .AnyAsync(c => c.ParentRecipeId == request.RecipeId
                        && c.IngredientId == request.IngredientId, ct)
            .ConfigureAwait(false);
        if (dup)
            throw new HomeChefPro.Domain.Common.DomainException(
                $"Recipe already contains ingredient {request.IngredientId}.");

        var component = RecipeComponent.ForIngredient(
            parentRecipeId: request.RecipeId,
            ingredientId: request.IngredientId,
            quantity: request.Quantity,
            notes: request.Notes,
            displayOrder: request.DisplayOrder,
            clock: clock,
            id: null);

        db.RecipeComponents.Add(component);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return component.Id;
    }
}
