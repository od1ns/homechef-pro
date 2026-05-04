using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Commands.CreateGuestOrder;

public sealed record OrderLineInput(Guid DishId, int Quantity, string? ItemNotes = null);

public sealed record CreateGuestOrderCommand(
    string GuestFullName,
    string GuestPhone,
    string DeliveryType,
    IReadOnlyList<OrderLineInput> Items,
    string? DeliveryAddress = null,
    string? DeliveryInstructions = null,
    string? ContactPhone = null,
    DateTimeOffset? ScheduledFor = null,
    string? CustomerNotes = null) : IRequest<CreateGuestOrderResult>;

/// <summary>
/// Resultado del creating order. F-24: incluye AccessToken para que el cliente lo guarde
/// y lo envie en GET subsiguientes.
/// </summary>
public sealed record CreateGuestOrderResult(Guid Id, string AccessToken);

public sealed class CreateGuestOrderValidator : AbstractValidator<CreateGuestOrderCommand>
{
    public CreateGuestOrderValidator()
    {
        RuleFor(x => x.GuestFullName).NotEmpty().MaximumLength(160);
        RuleFor(x => x.GuestPhone).NotEmpty().MaximumLength(30);
        RuleFor(x => x.DeliveryType)
            .Must(d => EnumDbMap<DeliveryType>.TryFromDb(d, out _))
            .WithMessage("DeliveryType must be 'pickup' or 'third_party'.");
        When(x => x.DeliveryType == "third_party", () =>
        {
            RuleFor(x => x.DeliveryAddress).NotEmpty()
                .WithMessage("Delivery address is required for third-party delivery.");
        });
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DishId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
        });
    }
}

public sealed class CreateGuestOrderHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<CreateGuestOrderCommand, CreateGuestOrderResult>
{
    public async Task<CreateGuestOrderResult> Handle(CreateGuestOrderCommand request, CancellationToken ct)
    {
        var dishIds = request.Items.Select(i => i.DishId).Distinct().ToArray();
        var dishes = await db.Recipes
            .AsNoTracking()
            .Where(r => dishIds.Contains(r.Id) && !r.IsSubRecipe)
            .ToListAsync(ct).ConfigureAwait(false);

        if (dishes.Count != dishIds.Length)
        {
            var missing = dishIds.Except(dishes.Select(d => d.Id)).First();
            throw new NotFoundException(nameof(Recipe), missing);
        }

        if (dishes.Any(d => !d.IsActive || d.IsOutOfStock))
        {
            var unavailable = dishes.First(d => !d.IsActive || d.IsOutOfStock);
            throw new InvalidOperationException(
                $"Dish '{unavailable.Name}' is not available right now.");
        }

        // Snapshot today's exchange rate if there is one for the order date.
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var rate = await db.ExchangeRates
            .AsNoTracking()
            .Where(r => r.EffectiveDate <= today)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        var guest = GuestCustomer.Create(request.GuestFullName, request.GuestPhone, clock);
        db.GuestCustomers.Add(guest);

        var order = Order.CreateForGuest(
            guestCustomerId: guest.Id,
            deliveryType: EnumDbMap<DeliveryType>.FromDb(request.DeliveryType),
            deliveryAddress: request.DeliveryAddress,
            deliveryInstructions: request.DeliveryInstructions,
            contactPhone: request.ContactPhone ?? request.GuestPhone,
            scheduledFor: request.ScheduledFor,
            customerNotes: request.CustomerNotes,
            exchangeRateId: rate?.Id,
            rateVesPerUsd: rate?.RateVesPerUsd,
            clock: clock);

        foreach (var line in request.Items)
        {
            var dish = dishes.First(d => d.Id == line.DishId);
            order.AddItem(
                dishId: dish.Id,
                dishNameSnapshot: dish.Name,
                unitPriceUsd: dish.SellingPriceUsd ?? 0m,
                quantity: line.Quantity,
                itemNotes: line.ItemNotes,
                clock: clock);
        }

        if (rate is not null)
            order.ApplyExchangeSnapshot(rate.Id, rate.RateVesPerUsd);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // F-24: el trigger SQL `trg_orders_access_token` asigna el token en BEFORE INSERT.
        // EF con ValueGeneratedOnAdd a veces no lee de vuelta el valor cuando la propiedad
        // CLR es string vacia (depende de sentinels). Re-read explicito garantiza que
        // tengamos el valor real para retornarlo al cliente.
        var generated = await db.Orders.AsNoTracking()
            .Where(o => o.Id == order.Id)
            .Select(o => new { o.AccessToken, o.OrderNumber })
            .FirstAsync(ct).ConfigureAwait(false);
        return new CreateGuestOrderResult(order.Id, generated.AccessToken);
    }
}
