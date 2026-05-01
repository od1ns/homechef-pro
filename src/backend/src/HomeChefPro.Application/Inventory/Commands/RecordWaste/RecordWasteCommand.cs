using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Inventory;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Inventory.Commands.RecordWaste;

public sealed record RecordWasteCommand(
    Guid IngredientId,
    decimal QuantityUseUnit,
    string Reason,
    string? Notes = null,
    DateTimeOffset? RecordedAt = null) : IRequest<Guid>;

public sealed class RecordWasteValidator : AbstractValidator<RecordWasteCommand>
{
    public RecordWasteValidator()
    {
        RuleFor(x => x.IngredientId).NotEmpty();
        RuleFor(x => x.QuantityUseUnit).GreaterThan(0);
        RuleFor(x => x.Reason).NotEmpty()
            .Must(r => EnumDbMap<WasteReason>.TryFromDb(r, out _))
            .WithMessage("Reason must be one of: "
                + "spoiled, burnt, dropped, expired, over_prepped, theft, other.");
    }
}

public sealed class RecordWasteHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<RecordWasteCommand, Guid>
{
    public async Task<Guid> Handle(RecordWasteCommand request, CancellationToken ct)
    {
        // Make sure the ingredient exists; the SQL trigger will reject the insert
        // if the waste exceeds current stock, but we surface that as a 404/409 here
        // for a friendlier API contract.
        var stock = await db.Ingredients
            .AsNoTracking()
            .Where(i => i.Id == request.IngredientId)
            .Select(i => (decimal?)i.CurrentStockUseUnit)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (stock is null)
            throw new NotFoundException(nameof(Ingredient), request.IngredientId);

        if (request.QuantityUseUnit > stock.Value)
            throw new DomainException(
                $"No se puede registrar una merma de {request.QuantityUseUnit} si "
                + $"el stock actual es {stock.Value}.");

        var waste = IngredientWaste.Record(
            ingredientId: request.IngredientId,
            quantityUseUnit: request.QuantityUseUnit,
            reason: EnumDbMap<WasteReason>.FromDb(request.Reason),
            recordedBy: currentUser.RequireUserId(),
            notes: request.Notes,
            recordedAt: request.RecordedAt,
            clock: clock);

        db.IngredientWaste.Add(waste);
        // Trigger fn_apply_waste_to_stock decrements stock + logs an inventory_movement.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return waste.Id;
    }
}
