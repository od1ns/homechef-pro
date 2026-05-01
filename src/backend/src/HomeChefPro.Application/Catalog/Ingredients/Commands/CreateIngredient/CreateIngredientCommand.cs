using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Common;
using MediatR;

namespace HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;

public sealed record CreateIngredientCommand(
    string Name,
    string UseUnit,
    decimal ReorderPointUseUnit = 0m,
    decimal MinimumStockUseUnit = 0m,
    string? Description = null) : IRequest<Guid>;

public sealed class CreateIngredientValidator : AbstractValidator<CreateIngredientCommand>
{
    public CreateIngredientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.UseUnit).NotEmpty()
            .Must(u => EnumDbMap<UseUnit>.TryFromDb(u, out _))
            .WithMessage("UseUnit must be one of 'g', 'ml', 'unit'.");
        RuleFor(x => x.ReorderPointUseUnit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockUseUnit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Description).MaximumLength(4000);
    }
}

public sealed class CreateIngredientHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<CreateIngredientCommand, Guid>
{
    public async Task<Guid> Handle(CreateIngredientCommand request, CancellationToken ct)
    {
        var useUnit = EnumDbMap<UseUnit>.FromDb(request.UseUnit);
        var ingredient = Ingredient.Create(
            name: request.Name,
            useUnit: useUnit,
            reorderPoint: request.ReorderPointUseUnit,
            minimumStock: request.MinimumStockUseUnit,
            description: request.Description,
            clock: clock);
        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return ingredient.Id;
    }
}
