using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Common;
using MediatR;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;

public sealed record CreateDishCommand(
    string Name,
    decimal SellingPriceUsd,
    int PrepTimeMinutes = 0,
    string MenuType = "fixed",
    DateTimeOffset? SpecialFrom = null,
    DateTimeOffset? SpecialTo = null,
    string? Description = null,
    string? Category = null) : IRequest<Guid>;

public sealed class CreateDishValidator : AbstractValidator<CreateDishCommand>
{
    public CreateDishValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SellingPriceUsd).GreaterThan(0);
        RuleFor(x => x.PrepTimeMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MenuType).NotEmpty()
            .Must(m => EnumDbMap<MenuType>.TryFromDb(m, out _))
            .WithMessage("MenuType must be 'fixed' or 'daily_special'.");
        When(x => x.MenuType == "daily_special", () =>
        {
            RuleFor(x => x.SpecialFrom).NotNull();
            RuleFor(x => x.SpecialTo).NotNull()
                .GreaterThan(x => x.SpecialFrom);
        });
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Category).MaximumLength(60);
    }
}

public sealed class CreateDishHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<CreateDishCommand, Guid>
{
    public async Task<Guid> Handle(CreateDishCommand request, CancellationToken ct)
    {
        var recipe = Recipe.CreateDish(
            name: request.Name,
            sellingPriceUsd: request.SellingPriceUsd,
            prepTimeMinutes: request.PrepTimeMinutes,
            menuType: EnumDbMap<MenuType>.FromDb(request.MenuType),
            specialFrom: request.SpecialFrom,
            specialTo: request.SpecialTo,
            description: request.Description,
            category: request.Category,
            clock: clock);

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return recipe.Id;
    }
}
