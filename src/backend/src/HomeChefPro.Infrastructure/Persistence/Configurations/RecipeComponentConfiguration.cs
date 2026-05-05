using HomeChefPro.Domain.Catalog.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class RecipeComponentConfiguration : IEntityTypeConfiguration<RecipeComponent>
{
    public void Configure(EntityTypeBuilder<RecipeComponent> builder)
    {
        builder.ToTable("recipe_components", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

        builder.Property(x => x.ParentRecipeId).IsRequired();
        builder.Property(x => x.IngredientId);
        builder.Property(x => x.SubRecipeId);
        builder.Property(x => x.Quantity).HasColumnType("numeric(14,4)");
        builder.Property(x => x.Notes).HasMaxLength(200);
        builder.Property(x => x.DisplayOrder);
        builder.Property(x => x.CreatedAt);

        builder.Ignore(x => x.IsIngredient);
        builder.Ignore(x => x.IsSubRecipe);
    }
}
