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

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);
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
        builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        // Pasada C / H-02: UNIQUE compuesto con chef_id.
        builder.HasIndex(x => new { x.ChefId, x.Name }).IsUnique();

        builder.HasMany(x => x.Presentations)
               .WithOne()
               .HasForeignKey(p => p.IngredientId)
               .OnDelete(DeleteBehavior.Restrict);

        // F-26 (Tier 2): optimistic concurrency con xmin de Postgres.
        builder.UseXminConcurrencyToken();

        builder.Ignore(x => x.DomainEvents);
    }
}
