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

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

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
