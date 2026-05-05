using HomeChefPro.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class IngredientPurchaseConfiguration : IEntityTypeConfiguration<IngredientPurchase>
{
    public void Configure(EntityTypeBuilder<IngredientPurchase> builder)
    {
        builder.ToTable("ingredient_purchases", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

        builder.Property(x => x.IngredientId).IsRequired();
        builder.Property(x => x.PresentationId).IsRequired();
        builder.Property(x => x.QuantityPurchased).HasColumnType("numeric(12,4)");
        builder.Property(x => x.UnitPriceUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.TotalCostUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.Supplier).HasMaxLength(160);
        builder.Property(x => x.Reference).HasMaxLength(120);
        builder.Property(x => x.PurchasedAt);
        builder.Property(x => x.RecordedBy).IsRequired();
        builder.Property(x => x.Notes);

        builder.Ignore(x => x.DomainEvents);
    }
}
