using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.UpdateRecipeTags;

/// <summary>
/// Etapa 3: reemplaza todas las etiquetas de un plato.
/// Enviar lista vacía elimina todas las etiquetas.
/// </summary>
public sealed record UpdateRecipeTagsCommand(
    Guid RecipeId,
    IReadOnlyList<string> Tags) : IRequest;

public sealed class UpdateRecipeTagsValidator : AbstractValidator<UpdateRecipeTagsCommand>
{
    public UpdateRecipeTagsValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.Tags).NotNull();
        RuleForEach(x => x.Tags)
            .NotEmpty()
            .MaximumLength(50)
            .Must(t => Recipe.AllowedTags.Contains(t.Trim().ToLowerInvariant()))
            .WithMessage($"Tag no válido. Valores permitidos: {string.Join(", ", Recipe.AllowedTags)}.");
    }
}

public sealed class UpdateRecipeTagsHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateRecipeTagsCommand>
{
    public async Task Handle(UpdateRecipeTagsCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes
            .FirstOrDefaultAsync(r => r.Id == request.RecipeId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        recipe.UpdateTags(request.Tags, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
