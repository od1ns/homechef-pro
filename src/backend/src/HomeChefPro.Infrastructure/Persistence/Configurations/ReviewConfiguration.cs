using HomeChefPro.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("reviews", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.DishId).IsRequired();

        builder.Property(x => x.Rating).HasColumnType("smallint");
        builder.Property(x => x.Comment);

        builder.Property(x => x.IsVisible);
        builder.Property(x => x.ModeratedBy);
        builder.Property(x => x.ModeratedAt);
        builder.Property(x => x.ModerationNote);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.HasIndex(x => new { x.UserId, x.OrderId, x.DishId }).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
