using FluentValidation.TestHelper;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateDish;
using HomeChefPro.Application.Catalog.Recipes.Commands.CreateSubRecipe;
using HomeChefPro.Application.Orders.Commands.AdvanceOrderStatus;
using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;

namespace HomeChefPro.Application.Tests.Validators;

public class CreateDishValidatorTests
{
    private readonly CreateDishValidator _v = new();

    [Fact]
    public void Daily_special_requires_window()
    {
        var r = _v.TestValidate(new CreateDishCommand(
            Name: "X", SellingPriceUsd: 5m, MenuType: "daily_special"));
        r.ShouldHaveValidationErrorFor(x => x.SpecialFrom);
        r.ShouldHaveValidationErrorFor(x => x.SpecialTo);
    }

    [Fact]
    public void Special_window_must_be_chronological()
    {
        var to = DateTimeOffset.UtcNow.AddHours(1);
        var from = to.AddHours(2);     // from > to
        var r = _v.TestValidate(new CreateDishCommand(
            Name: "X", SellingPriceUsd: 5m, MenuType: "daily_special",
            SpecialFrom: from, SpecialTo: to));
        r.ShouldHaveValidationErrorFor(x => x.SpecialTo);
    }

    [Fact]
    public void Free_priced_dish_fails()
    {
        var r = _v.TestValidate(new CreateDishCommand(Name: "X", SellingPriceUsd: 0));
        r.ShouldHaveValidationErrorFor(x => x.SellingPriceUsd);
    }
}

public class CreateSubRecipeValidatorTests
{
    private readonly CreateSubRecipeValidator _v = new();

    [Fact]
    public void Yield_must_be_positive()
    {
        var r = _v.TestValidate(new CreateSubRecipeCommand(
            Name: "Sofrito", YieldQuantity: 0, YieldUnit: "g"));
        r.ShouldHaveValidationErrorFor(x => x.YieldQuantity);
    }

    [Fact]
    public void Bad_yield_unit_fails()
    {
        var r = _v.TestValidate(new CreateSubRecipeCommand(
            Name: "Sofrito", YieldQuantity: 100, YieldUnit: "cup"));
        r.ShouldHaveValidationErrorFor(x => x.YieldUnit);
    }
}

public class CreateGuestOrderValidatorTests
{
    private readonly CreateGuestOrderValidator _v = new();

    [Fact]
    public void Third_party_without_address_fails()
    {
        var r = _v.TestValidate(new CreateGuestOrderCommand(
            GuestFullName: "X",
            GuestPhone: "+58 412-0000000",
            DeliveryType: "third_party",
            Items: [new OrderLineInput(Guid.NewGuid(), 1)]));
        r.ShouldHaveValidationErrorFor(x => x.DeliveryAddress);
    }

    [Fact]
    public void Empty_items_fails()
    {
        var r = _v.TestValidate(new CreateGuestOrderCommand(
            GuestFullName: "X",
            GuestPhone: "+58 412-0000000",
            DeliveryType: "pickup",
            Items: []));
        r.ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void Invalid_delivery_type_fails()
    {
        var r = _v.TestValidate(new CreateGuestOrderCommand(
            GuestFullName: "X",
            GuestPhone: "+58 412-0000000",
            DeliveryType: "drone",
            Items: [new OrderLineInput(Guid.NewGuid(), 1)]));
        r.ShouldHaveValidationErrorFor(x => x.DeliveryType);
    }
}

public class AdvanceOrderStatusValidatorTests
{
    private readonly AdvanceOrderStatusValidator _v = new();

    [Fact]
    public void Cancel_without_reason_fails()
    {
        var r = _v.TestValidate(new AdvanceOrderStatusCommand(
            OrderId: Guid.NewGuid(), Target: "cancelled"));
        r.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Unknown_target_fails()
    {
        var r = _v.TestValidate(new AdvanceOrderStatusCommand(
            OrderId: Guid.NewGuid(), Target: "wat"));
        r.ShouldHaveValidationErrorFor(x => x.Target);
    }
}

public class SubmitPaymentProofValidatorTests
{
    private readonly SubmitPaymentProofValidator _v = new();

    [Fact]
    public void VES_payment_requires_exchange_rate()
    {
        var r = _v.TestValidate(new SubmitPaymentProofCommand(
            OrderId: Guid.NewGuid(),
            Method: "pago_movil",
            AmountUsd: 5m,
            PaidCurrency: "VES",
            AmountPaidCurrency: 200m,
            ExchangeRateUsed: null));
        r.ShouldHaveValidationErrorFor(x => x.ExchangeRateUsed);
    }

    [Fact]
    public void USD_payment_does_not_require_rate()
    {
        var r = _v.TestValidate(new SubmitPaymentProofCommand(
            OrderId: Guid.NewGuid(),
            Method: "zelle",
            AmountUsd: 5m,
            PaidCurrency: "USD",
            AmountPaidCurrency: 5m));
        r.ShouldNotHaveValidationErrorFor(x => x.ExchangeRateUsed);
    }
}
