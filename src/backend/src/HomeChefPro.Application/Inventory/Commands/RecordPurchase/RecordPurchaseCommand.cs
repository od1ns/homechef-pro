using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Inventory.Commands.RecordPurchase;

public sealed record RecordPurchaseCommand(
    Guid IngredientId,
    Guid PresentationId,
    decimal QuantityPurchased,
    decimal UnitPriceUsd,
    string? Supplier = null,
    string? Reference = null,
    string? Notes = null,
    DateTimeOffset? PurchasedAt = null) : IRequest<Guid>;

public sealed class RecordPurchaseValidator : AbstractValidator<RecordPurchaseCommand>
{
    public RecordPurchaseValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.PresentationId).NotEmpty();
        RuleFor(x => x.QuantityPurchased).GreaterThan(0);
        RuleFor(x => x.UnitPriceUsd).GreaterThan(0);
        RuleFor(x => x.Supplier).MaximumLength(160);
        RuleFor(x => x.Reference).MaximumLength(120);
    }
}

public sealed class RecordPurchaseHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<RecordPurchaseCommand, Guid>
{
    public async Task<Guid> Handle(RecordPurchaseCommand request, CancellationToken ct)
    {
        var presentation = await db.IngredientPresentations
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PresentationId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(
                nameof(IngredientPresentation), request.PresentationId);

        if (presentation.IngredientId != request.IngredientId)
            throw new Common.Exceptions.ValidationException(
            [
                new FluentValidation.Results.ValidationFailure(
                    nameof(request.PresentationId),
                    "Presentation does not belong to the given ingredient."),
            ]);

        var purchase = IngredientPurchase.Record(
            ingredientId: request.IngredientId,
            presentationId: request.PresentationId,
            quantityPurchased: request.QuantityPurchased,
            unitPriceUsd: request.UnitPriceUsd,
            recordedBy: currentUser.RequireUserId(),
            supplier: request.Supplier,
            reference: request.Reference,
            notes: request.Notes,
            purchasedAt: request.PurchasedAt,
            clock: clock);

        db.IngredientPurchases.Add(purchase);
        // The trigger fn_apply_purchase_to_stock updates stock + avg cost in the same transaction.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return purchase.Id;
    }
}
