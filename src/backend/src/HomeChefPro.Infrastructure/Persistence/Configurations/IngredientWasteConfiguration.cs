using HomeChefPro.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class IngredientWasteConfiguration : IEntityTypeConfiguration<IngredientWaste>
{
    public void Configure(EntityTypeBuilder<IngredientWaste> builder)
    {
        builder.ToTable("ingredient_waste", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IngredientId).IsRequired();
        builder.Property(x => x.QuantityUseUnit).HasColumnType("numeric(14,4)");
        builder.Property(x => x.EstimatedCostUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.Reason).HasMaxLength(30).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.Notes);
        builder.Property(x => x.RecordedBy).IsRequired();
        builder.Property(x => x.RecordedAt);

        builder.Ignore(x => x.DomainEvents);
    }
}
