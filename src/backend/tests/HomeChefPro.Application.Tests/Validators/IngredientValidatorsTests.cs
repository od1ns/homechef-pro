using FluentAssertions;
using FluentValidation.TestHelper;
using HomeChefPro.Application.Catalog.Ingredients.Commands.AddPresentation;
using HomeChefPro.Application.Catalog.Ingredients.Commands.CreateIngredient;
using HomeChefPro.Application.Catalog.Ingredients.Commands.UpdateReorderThresholds;

namespace HomeChefPro.Application.Tests.Validators;

public class CreateIngredientValidatorTests
{
    private readonly CreateIngredientValidator _v = new();

    [Fact]
    public void Empty_name_fails()
    {
        var r = _v.TestValidate(new CreateIngredientCommand(Name: "", UseUnit: "g"));
        r.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Invalid_use_unit_fails()
    {
        var r = _v.TestValidate(new CreateIngredientCommand(Name: "x", UseUnit: "tbsp"));
        r.ShouldHaveValidationErrorFor(x => x.UseUnit);
    }

    [Theory]
    [InlineData("g")]
    [InlineData("ml")]
    [InlineData("unit")]
    public void Valid_use_units_pass(string unit)
    {
        var r = _v.TestValidate(new CreateIngredientCommand(Name: "x", UseUnit: unit));
        r.ShouldNotHaveValidationErrorFor(x => x.UseUnit);
    }

    [Fact]
    public void Negative_thresholds_fail()
    {
        var r = _v.TestValidate(new CreateIngredientCommand(
            Name: "x", UseUnit: "g", ReorderPointUseUnit: -1m));
        r.ShouldHaveValidationErrorFor(x => x.ReorderPointUseUnit);
    }
}

public class AddPresentationValidatorTests
{
    private readonly AddPresentationValidator _v = new();

    [Fact]
    public void Zero_quantity_fails()
    {
        var r = _v.TestValidate(new AddPresentationCommand(
            IngredientId: Guid.NewGuid(),
            Name: "Saco",
            PurchaseUnit: "kg",
            PurchaseQuantity: 0,
            ConversionToUseUnit: 1000));
        r.ShouldHaveValidationErrorFor(x => x.PurchaseQuantity);
    }

    [Fact]
    public void Unknown_purchase_unit_fails()
    {
        var r = _v.TestValidate(new AddPresentationCommand(
            IngredientId: Guid.NewGuid(),
            Name: "Saco",
            PurchaseUnit: "barril",
            PurchaseQuantity: 1,
            ConversionToUseUnit: 1));
        r.ShouldHaveValidationErrorFor(x => x.PurchaseUnit);
    }
}

public class UpdateReorderThresholdsValidatorTests
{
    [Fact]
    public void Empty_id_fails()
    {
        var v = new UpdateReorderThresholdsValidator();
        var r = v.TestValidate(new UpdateReorderThresholdsCommand(
            IngredientId: Guid.Empty,
            ReorderPointUseUnit: 0,
            MinimumStockUseUnit: 0));
        r.ShouldHaveValidationErrorFor(x => x.IngredientId);
    }
}
