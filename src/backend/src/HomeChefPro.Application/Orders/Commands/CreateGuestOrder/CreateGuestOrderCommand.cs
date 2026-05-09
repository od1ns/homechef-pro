using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Catalog.Recipes;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Commands.CreateGuestOrder;

/// <summary>Etapa 2: modificador seleccionado por el cliente para una linea de pedido.</summary>
public sealed record OrderLineModifierInput(Guid ModifierId, int Quantity);

public sealed record OrderLineInput(
    Guid DishId,
    int Quantity,
    string? ItemNotes = null,
    IReadOnlyList<OrderLineModifierInput>? Modifiers = null);  // Etapa 2

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
        // F-31 (Tier 2): limites para evitar abuso.
        RuleFor(x => x.Items)
            .Must(items => items.Count <= 30)
            .WithMessage("An order can have at most 30 distinct items.");
        RuleFor(x => x.Items)
            .Must(items => items.Sum(i => i.Quantity) <= 200)
            .WithMessage("Total quantity across all items cannot exceed 200 units.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.DishId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.Quantity).LessThanOrEqualTo(50)
                .WithMessage("Quantity per item cannot exceed 50.");
            item.RuleFor(i => i.ItemNotes).MaximumLength(500);
            // Etapa 2: validar modificadores por linea
            item.RuleForEach(i => i.Modifiers).ChildRules(mod =>
            {
                mod.RuleFor(m => m.ModifierId).NotEmpty();
                mod.RuleFor(m => m.Quantity).GreaterThanOrEqualTo(0);
            });
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

        // Etapa 2: cargar recetas con sus modificadores activos para validar + snapshot
        var dishes = await db.Recipes
            .Include(r => r.Modifiers)
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

            // Etapa 2: calcular delta de modificadores para incluir en unit_price
            var modifierDelta = 0m;
            var activeModifiers = dish.Modifiers.Where(m => m.IsActive).ToDictionary(m => m.Id);

            if (line.Modifiers is { Count: > 0 })
            {
                foreach (var modInput in line.Modifiers)
                {
                    if (!activeModifiers.TryGetValue(modInput.ModifierId, out var mod))
                        throw new NotFoundException(nameof(RecipeModifier), modInput.ModifierId);
                    if (modInput.Quantity < mod.MinQty || modInput.Quantity > mod.MaxQty)
                        throw new InvalidOperationException(
                            $"Cantidad {modInput.Quantity} para '{mod.Name}' fuera del rango [{mod.MinQty},{mod.MaxQty}].");
                    modifierDelta += modInput.Quantity * mod.PriceDeltaUsd;
                }
            }

            var unitPrice = (dish.SellingPriceUsd ?? 0m) + modifierDelta;
            var orderItem = order.AddItem(
                dishId: dish.Id,
                dishNameSnapshot: dish.Name,
                unitPriceUsd: unitPrice,
                quantity: line.Quantity,
                itemNotes: line.ItemNotes,
                clock: clock);

            // Etapa 2: adjuntar snapshots de modificadores al item
            if (line.Modifiers is { Count: > 0 })
            {
                foreach (var modInput in line.Modifiers.Where(m => m.Quantity > 0))
                {
                    var mod = activeModifiers[modInput.ModifierId];
                    orderItem.AddModifierSnapshot(
                        modifierId: mod.Id,
                        modifierName: mod.Name,
                        qty: modInput.Quantity,
                        priceDelta: mod.PriceDeltaUsd);
                }
            }
        }

        if (rate is not null)
            order.ApplyExchangeSnapshot(rate.Id, rate.RateVesPerUsd);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // F-24: re-read del access_token generado por trigger SQL.
        var generated = await db.Orders.AsNoTracking()
            .Where(o => o.Id == order.Id)
            .Select(o => new { o.AccessToken, o.OrderNumber })
            .FirstAsync(ct).ConfigureAwait(false);
        return new CreateGuestOrderResult(order.Id, generated.AccessToken);
    }
}
