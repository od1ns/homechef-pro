using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using MediatR;

namespace HomeChefPro.Application.Catalog.Ingredients.Commands.UpdateReorderThresholds;

public sealed record UpdateReorderThresholdsCommand(
    Guid IngredientId,
    decimal ReorderPointUseUnit,
    decimal MinimumStockUseUnit) : IRequest;

public sealed class UpdateReorderThresholdsValidator : AbstractValidator<UpdateReorderThresholdsCommand>
{
    public UpdateReorderThresholdsValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.ReorderPointUseUnit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinimumStockUseUnit).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateReorderThresholdsHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<UpdateReorderThresholdsCommand>
{
    public async Task Handle(UpdateReorderThresholdsCommand request, CancellationToken ct)
    {
        var ingredient = await db.Ingredients.FindAsync([request.IngredientId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        ingredient.UpdateReorderThresholds(
            request.ReorderPointUseUnit,
            request.MinimumStockUseUnit,
            clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
