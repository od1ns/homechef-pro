using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.UpdateRecipeImage;

/// <summary>
/// Etapa 1: actualiza el imageUrl del recipe. La URL viene del storage tras el
/// upload del archivo. Si imageUrl es null/empty, limpia la imagen del plato.
/// </summary>
public sealed record UpdateRecipeImageCommand(Guid RecipeId, string? ImageUrl) : IRequest<Unit>;

public sealed class UpdateRecipeImageValidator : AbstractValidator<UpdateRecipeImageCommand>
{
    public UpdateRecipeImageValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.ImageUrl).MaximumLength(500);
    }
}

public sealed class UpdateRecipeImageHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateRecipeImageCommand, Unit>
{
    public async Task<Unit> Handle(UpdateRecipeImageCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        recipe.UpdateImage(request.ImageUrl, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
