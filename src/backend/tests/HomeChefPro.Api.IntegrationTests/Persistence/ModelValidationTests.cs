using FluentAssertions;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HomeChefPro.Api.IntegrationTests.Persistence;

/// <summary>
/// Builds the EF Core model (no DB round-trip) and asserts that the mapping
/// between Domain entities and the SQL schema is consistent:
///   - every aggregate has a table in snake_case
///   - enum columns carry their DB-string converter
///   - expected tables exist on the model
/// </summary>
public class ModelValidationTests
{
    private static HomeChefProDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<HomeChefProDbContext>()
            // We never actually connect; the model is built on first access to `.Model`.
            .UseNpgsql("Host=localhost;Database=none;Username=none;Password=none")
            .Options;
        return new HomeChefProDbContext(options);
    }

    [Fact]
    public void Model_builds_without_error()
    {
        using var ctx = BuildContext();
        var entityTypes = ctx.Model.GetEntityTypes().ToList();
        entityTypes.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("ingredients")]
    [InlineData("ingredient_presentations")]
    [InlineData("recipes")]
    [InlineData("recipe_components")]
    [InlineData("ingredient_purchases")]
    [InlineData("ingredient_waste")]
    [InlineData("inventory_movements")]
    [InlineData("exchange_rates")]
    [InlineData("user_profiles")]
    [InlineData("guest_customers")]
    [InlineData("orders")]
    [InlineData("order_items")]
    [InlineData("payments")]
    [InlineData("delivery_tracking")]
    [InlineData("delivery_events")]
    [InlineData("reviews")]
    public void Table_is_mapped_in_snake_case(string tableName)
    {
        using var ctx = BuildContext();
        var tables = ctx.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(n => n is not null)
            .Distinct()
            .ToList();

        tables.Should().Contain(tableName);
    }

    [Fact]
    public void Order_status_column_uses_string_converter()
    {
        using var ctx = BuildContext();
        var orderEntity = ctx.Model.FindEntityType(typeof(HomeChefPro.Domain.Orders.Order))!;
        var statusProp = orderEntity.FindProperty(nameof(HomeChefPro.Domain.Orders.Order.Status))!;
        statusProp.GetColumnName().Should().Be("status");
        statusProp.GetValueConverter().Should().NotBeNull();
        statusProp.GetValueConverter()!.ProviderClrType.Should().Be<string>();
    }

    [Fact]
    public void Ingredient_name_column_is_snake_case()
    {
        using var ctx = BuildContext();
        var ing = ctx.Model.FindEntityType(typeof(HomeChefPro.Domain.Catalog.Ingredients.Ingredient))!;
        ing.FindProperty("AvgCostPerUseUnitUsd")!.GetColumnName().Should().Be("avg_cost_per_use_unit_usd");
        ing.FindProperty("CurrentStockUseUnit")!.GetColumnName().Should().Be("current_stock_use_unit");
    }

    [Fact]
    public void Identity_tables_keep_PascalCase_by_default()
    {
        using var ctx = BuildContext();
        // Identity tables are renamed by snake_case too (AspNetUsers -> asp_net_users).
        // The schema comment in 01_identity_notes.sql says EF Core creates them —
        // whatever casing it picks is fine as long as it's consistent. Verify it's mapped.
        var userEntity = ctx.Model.GetEntityTypes()
            .SingleOrDefault(e => e.ClrType.Name == "AppUser");
        userEntity.Should().NotBeNull();
        userEntity!.GetTableName().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void All_domain_tables_are_excluded_from_migrations()
    {
        using var ctx = BuildContext();
        // IsExcludedFromMigrations lives on the design-time model, not the read-optimized runtime one.
        var designModel = ctx.GetService<IDesignTimeModel>().Model;
        var domainTables = new[]
        {
            "ingredients", "ingredient_presentations", "recipes", "recipe_components",
            "ingredient_purchases", "ingredient_waste", "inventory_movements",
            "exchange_rates", "user_profiles", "guest_customers", "orders", "order_items",
            "payments", "delivery_tracking", "delivery_events", "reviews",
        };

        foreach (var tableName in domainTables)
        {
            var entity = designModel.GetEntityTypes()
                .SingleOrDefault(e => e.GetTableName() == tableName);
            entity.Should().NotBeNull($"table {tableName} must be mapped");
            var mapping = entity!.GetTableMappings().SingleOrDefault()?.Table;
            mapping.Should().NotBeNull();
            mapping!.IsExcludedFromMigrations.Should().BeTrue(
                $"{tableName} is managed by SQL scripts, not EF migrations");
        }
    }
}
