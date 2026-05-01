using FluentAssertions;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Tests.Catalog;

public class IngredientTests
{
    [Fact]
    public void Create_trims_name_and_defaults_active()
    {
        var ing = Ingredient.Create("  Tomate  ", UseUnit.Gram);
        ing.Name.Should().Be("Tomate");
        ing.IsActive.Should().BeTrue();
        ing.CurrentStockUseUnit.Should().Be(0);
    }

    [Fact]
    public void Rejects_empty_name()
    {
        var act = () => Ingredient.Create("  ", UseUnit.Gram);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Add_presentation_with_same_name_throws()
    {
        var ing = Ingredient.Create("Harina", UseUnit.Gram);
        ing.AddPresentation("Saco 50kg", PurchaseUnit.Kilogram, 50, 1000);
        var act = () => ing.AddPresentation(" saco 50kg ", PurchaseUnit.Kilogram, 50, 1000);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Presentation_converts_to_use_units()
    {
        var ing = Ingredient.Create("Harina", UseUnit.Gram);
        var pres = ing.AddPresentation("Saco 50kg", PurchaseUnit.Kilogram, 50, 1000);
        pres.ToUseUnits(quantityPurchased: 2).Should().Be(2 * 50 * 1000); // 2 sacos * 50 kg * 1000 g/kg = 100_000 g
    }

    [Fact]
    public void Stock_status_flags_work()
    {
        var ing = Ingredient.Create("Queso", UseUnit.Gram, reorderPoint: 5000, minimumStock: 1000);
        // use internal sync to simulate a trigger update
        typeof(Ingredient).GetMethod("SyncStockFromDatabase",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(ing, [500m, 0.02m, DateTimeOffset.UtcNow]);

        ing.IsBelowMinimumStock.Should().BeTrue();
        ing.IsBelowReorderPoint.Should().BeTrue();
        ing.IsOutOfStock.Should().BeFalse();
    }
}
