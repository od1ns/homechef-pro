using HomeChefPro.Domain.Catalog.Ingredients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class IngredientPresentationConfiguration : IEntityTypeConfiguration<IngredientPresentation>
{
    public void Configure(EntityTypeBuilder<IngredientPresentation> builder)
    {
        builder.ToTable("ingredient_presentations", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.PurchaseUnit).HasMaxLength(10).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.PurchaseQuantity).HasColumnType("numeric(10,4)");
        builder.Property(x => x.ConversionToUseUnit).HasColumnType("numeric(14,6)");
        builder.Property(x => x.LastPurchasePriceUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.IsActive);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.HasIndex(x => new { x.IngredientId, x.Name }).IsUnique();
    }
}
