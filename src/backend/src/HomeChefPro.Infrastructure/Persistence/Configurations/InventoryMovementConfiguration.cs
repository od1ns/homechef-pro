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

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

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
