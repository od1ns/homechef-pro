using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Common;
using MediatR;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.CreateSubRecipe;

public sealed record CreateSubRecipeCommand(
    string Name,
    decimal YieldQuantity,
    string YieldUnit,
    int PrepTimeMinutes = 0,
    string? Description = null,
    string? Category = null) : IRequest<Guid>;

public sealed class CreateSubRecipeValidator : AbstractValidator<CreateSubRecipeCommand>
{
    public CreateSubRecipeValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.YieldQuantity).GreaterThan(0);
        RuleFor(x => x.YieldUnit).NotEmpty()
            .Must(u => EnumDbMap<YieldUnit>.TryFromDb(u, out _))
            .WithMessage("YieldUnit must be 'g', 'ml', 'portion' or 'unit'.");
        RuleFor(x => x.PrepTimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Category).MaximumLength(60);
    }
}

public sealed class CreateSubRecipeHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<CreateSubRecipeCommand, Guid>
{
    public async Task<Guid> Handle(CreateSubRecipeCommand request, CancellationToken ct)
    {
        var recipe = Recipe.CreateSubRecipe(
            name: request.Name,
            yieldQuantity: request.YieldQuantity,
            yieldUnit: EnumDbMap<YieldUnit>.FromDb(request.YieldUnit),
            prepTimeMinutes: request.PrepTimeMinutes,
            description: request.Description,
            category: request.Category,
            clock: clock);

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return recipe.Id;
    }
}
