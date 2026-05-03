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
        // No rastreamos al ingredient (AsNoTracking) para evitar que EF emita
        // un UPDATE vacio que con el trigger BEFORE UPDATE rompe con
        // "0 rows affected" -> DbUpdateConcurrencyException.
        var ingredientExists = await db.Ingredients
            .AsNoTracking()
            .AnyAsync(i => i.Id == request.IngredientId, ct)
            .ConfigureAwait(false);

        if (!ingredientExists)
            throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        var trimmedName = request.Name?.Trim() ?? string.Empty;
        var nameTaken = await db.IngredientPresentations
            .AsNoTracking()
            .AnyAsync(p => p.IngredientId == request.IngredientId
                        && p.Name.ToLower() == trimmedName.ToLower(), ct)
            .ConfigureAwait(false);

        if (nameTaken)
            throw new HomeChefPro.Domain.Common.DomainException(
                $"Ingredient already has a presentation named '{trimmedName}'.");

        var presentation = IngredientPresentation.Create(
            ingredientId: request.IngredientId,
            name: trimmedName,
            purchaseUnit: EnumDbMap<PurchaseUnit>.FromDb(request.PurchaseUnit),
            purchaseQuantity: request.PurchaseQuantity,
            conversionToUseUnit: request.ConversionToUseUnit,
            lastPurchasePriceUsd: request.LastPurchasePriceUsd,
            clock: clock);

        db.IngredientPresentations.Add(presentation);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return presentation.Id;
    }
}
