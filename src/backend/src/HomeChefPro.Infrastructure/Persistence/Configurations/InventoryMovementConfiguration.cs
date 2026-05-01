using HomeChefPro.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IngredientId).IsRequired();
        builder.Property(x => x.MovementType).HasMaxLength(20).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.QuantityUseUnit).HasColumnType("numeric(14,4)");
        builder.Property(x => x.CostImpactUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.SourceTable).HasMaxLength(40).IsRequired();
        builder.Property(x => x.SourceId);
        builder.Property(x => x.ResultingStock).HasColumnType("numeric(14,4)");
        builder.Property(x => x.ResultingAvgCost).HasColumnType("numeric(14,6)");
        builder.Property(x => x.OccurredAt);
        builder.Property(x => x.Notes);
    }
}
