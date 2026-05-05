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

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

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
