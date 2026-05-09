using HomeChefPro.Domain.Catalog.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class RecipeModifierConfiguration : IEntityTypeConfiguration<RecipeModifier>
{
    public void Configure(EntityTypeBuilder<RecipeModifier> builder)
    {
        builder.ToTable("recipe_modifiers", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

        builder.Property(x => x.RecipeId).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DefaultQty).IsRequired();
        builder.Property(x => x.MinQty).IsRequired();
        builder.Property(x => x.MaxQty).IsRequired();
        builder.Property(x => x.PriceDeltaUsd).HasColumnType("numeric(12,4)").IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).Metadata
               .SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);
    }
}
