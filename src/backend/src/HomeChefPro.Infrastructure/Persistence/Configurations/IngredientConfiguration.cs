using HomeChefPro.Domain.Catalog.Ingredients;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("ingredients", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Description);

        builder.Property(x => x.UseUnit).HasMaxLength(10).IsRequired().HasEnumDbValueConversion();

        builder.Property(x => x.CurrentStockUseUnit).HasColumnType("numeric(14,4)");
        builder.Property(x => x.ReorderPointUseUnit).HasColumnType("numeric(14,4)");
        builder.Property(x => x.MinimumStockUseUnit).HasColumnType("numeric(14,4)");
        builder.Property(x => x.AvgCostPerUseUnitUsd).HasColumnType("numeric(14,6)");

        builder.Property(x => x.IsActive);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.Name).IsUnique();

        builder.HasMany(x => x.Presentations)
               .WithOne()
               .HasForeignKey(p => p.IngredientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(x => x.DomainEvents);
    }
}
