using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.AddSubRecipeComponent;

public sealed record AddSubRecipeComponentCommand(
    Guid RecipeId,
    Guid SubRecipeId,
    decimal Quantity,
    string? Notes = null,
    int DisplayOrder = 0) : IRequest<Guid>;

public sealed class AddSubRecipeComponentValidator : AbstractValidator<AddSubRecipeComponentCommand>
{
    public AddSubRecipeComponentValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.SubRecipeId).NotEmpty()
            .NotEqual(x => x.RecipeId)
            .WithMessage("A recipe cannot contain itself.");
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(200);
    }
}

public sealed class AddSubRecipeComponentHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<AddSubRecipeComponentCommand, Guid>
{
    public async Task<Guid> Handle(AddSubRecipeComponentCommand request, CancellationToken ct)
    {
        var recipeExists = await db.Recipes
            .AsNoTracking()
            .AnyAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false);
        if (!recipeExists)
            throw new NotFoundException(nameof(Recipe), request.RecipeId);

        var sub = await db.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.SubRecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.SubRecipeId);

        if (!sub.IsSubRecipe)
            throw new InvalidOperationException(
                $"Recipe {sub.Id} is not a sub-recipe and cannot be used as a component.");

        var dup = await db.RecipeComponents
            .AsNoTracking()
            .AnyAsync(c => c.ParentRecipeId == request.RecipeId
                        && c.SubRecipeId == request.SubRecipeId, ct)
            .ConfigureAwait(false);
        if (dup)
            throw new HomeChefPro.Domain.Common.DomainException(
                $"Recipe already contains sub-recipe {request.SubRecipeId}.");

        var component = RecipeComponent.ForSubRecipe(
            parentRecipeId: request.RecipeId,
            subRecipeId: request.SubRecipeId,
            quantity: request.Quantity,
            notes: request.Notes,
            displayOrder: request.DisplayOrder,
            clock: clock);

        db.RecipeComponents.Add(component);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return component.Id;
    }
}
