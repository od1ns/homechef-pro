using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using MediatR;

namespace HomeChefPro.Application.Catalog.Recipes.Commands.UpdateSellingPrice;

public sealed record UpdateSellingPriceCommand(Guid RecipeId, decimal SellingPriceUsd) : IRequest;

public sealed class UpdateSellingPriceValidator : AbstractValidator<UpdateSellingPriceCommand>
{
    public UpdateSellingPriceValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.SellingPriceUsd).GreaterThan(0);
    }
}

public sealed class UpdateSellingPriceHandler(IHomeChefProDbContext db, TimeProvider clock)
    : IRequestHandler<UpdateSellingPriceCommand>
{
    public async Task Handle(UpdateSellingPriceCommand request, CancellationToken ct)
    {
        var recipe = await db.Recipes.FindAsync([request.RecipeId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Recipe), request.RecipeId);

        recipe.UpdateSellingPrice(request.SellingPriceUsd, clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
