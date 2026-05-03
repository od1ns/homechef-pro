using FluentAssertions;
using HomeChefPro.Domain.Catalog.Recipes;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Api.IntegrationTests.Persistence;

/// <summary>
/// Smoke tests that validate the full pipeline: SQL schema + seeds + EF mapping.
/// Marked <c>[Trait("Category", "Integration")]</c> so they can be filtered in/out of CI runs.
/// Run explicitly with: <c>dotnet test --filter Category=Integration</c>
/// Requires Docker Desktop running.
/// </summary>
[Trait("Category", "Integration")]
[Collection("IntegrationDb")]
public class LiveDatabaseTests
{
    private readonly LiveDatabaseFixture _fixture;

    public LiveDatabaseTests(LiveDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Seed_produces_20_ingredients_and_5_recipes()
    {
        await using var db = _fixture.CreateContext();
        var ingredients = await db.Ingredients.CountAsync();
        var recipes = await db.Recipes.CountAsync();

        ingredients.Should().Be(20);
        recipes.Should().Be(5);
    }

    [Fact]
    public async Task Dishes_have_non_null_selling_price()
    {
        await using var db = _fixture.CreateContext();
        var dishes = await db.Recipes
            .Where(r => !r.IsSubRecipe)
            .ToListAsync();

        dishes.Should().NotBeEmpty();
        dishes.Should().OnlyContain(d => d.SellingPriceUsd.HasValue && d.SellingPriceUsd > 0);
    }

    [Fact]
    public async Task Sub_recipes_have_yield()
    {
        await using var db = _fixture.CreateContext();
        var subs = await db.Recipes
            .Where(r => r.IsSubRecipe)
            .ToListAsync();

        subs.Should().NotBeEmpty();
        subs.Should().OnlyContain(s =>
            s.YieldQuantity.HasValue
            && s.YieldQuantity > 0
            && s.YieldUnit.HasValue);
    }

    [Fact]
    public async Task Pabellon_criollo_is_mapped_and_loadable()
    {
        await using var db = _fixture.CreateContext();
        var pabellon = await db.Recipes
            .Include(r => r.Components)
            .FirstAsync(r => r.Name.StartsWith("Pabellón"));

        pabellon.Components.Should().NotBeEmpty();
        pabellon.MenuType.Should().BeOneOf(MenuType.Fixed, MenuType.DailySpecial);
    }
}
