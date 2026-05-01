using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Catalog.Ingredients.Commands.AddPresentation;

public sealed record AddPresentationCommand(
    Guid IngredientId,
    string Name,
    string PurchaseUnit,
    decimal PurchaseQuantity,
    decimal ConversionToUseUnit,
    decimal? LastPurchasePriceUsd = null) : IRequest<Guid>;

public sealed class AddPresentationValidator : AbstractValidator<AddPresentationCommand>
{
    public AddPresentationValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.PurchaseUnit).NotEmpty()
            .Must(u => EnumDbMap<PurchaseUnit>.TryFromDb(u, out _))
            .WithMessage("Unknown PurchaseUnit.");
        RuleFor(x => x.PurchaseQuantity).GreaterThan(0);
        RuleFor(x => x.ConversionToUseUnit).GreaterThan(0);
        RuleFor(x => x.LastPurchasePriceUsd).GreaterThanOrEqualTo(0)
            .When(x => x.LastPurchasePriceUsd.HasValue);
    }
}

public sealed class AddPresentationHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<AddPresentationCommand, Guid>
{
    public async Task<Guid> Handle(AddPresentationCommand request, CancellationToken ct)
    {
        var ingredient = await db.Ingredients
            .Include(i => i.Presentations)
            .FirstOrDefaultAsync(i => i.Id == request.IngredientId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        var presentation = ingredient.AddPresentation(
            name: request.Name,
            purchaseUnit: EnumDbMap<PurchaseUnit>.FromDb(request.PurchaseUnit),
            purchaseQuantity: request.PurchaseQuantity,
            conversionToUseUnit: request.ConversionToUseUnit,
            lastPurchasePriceUsd: request.LastPurchasePriceUsd,
            clock: clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return presentation.Id;
    }
}
