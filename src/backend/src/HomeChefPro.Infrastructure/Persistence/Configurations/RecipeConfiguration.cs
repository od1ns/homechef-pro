using HomeChefPro.Domain.Catalog.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description);
        builder.Property(x => x.Category).HasMaxLength(60);
        builder.Property(x => x.IsSubRecipe);

        builder.Property(x => x.ProcedureMarkdown);
        builder.Property(x => x.YieldQuantity).HasColumnType("numeric(12,4)");
        builder.Property(x => x.YieldUnit).HasMaxLength(10).HasEnumDbValueConversion();

        builder.Property(x => x.SuggestedPriceUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.SellingPriceUsd).HasColumnType("numeric(12,4)");

        builder.Property(x => x.PrepTimeMinutes);
        builder.Property(x => x.ImageUrl);

        builder.Property(x => x.IsActive);
        builder.Property(x => x.IsOutOfStock);

        builder.Property(x => x.MenuType).HasMaxLength(20).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.SpecialFrom);
        builder.Property(x => x.SpecialTo);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.HasMany(x => x.Components)
               .WithOne()
               .HasForeignKey(c => c.ParentRecipeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
