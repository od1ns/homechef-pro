using HomeChefPro.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("user_id");

        builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.DefaultPhone).HasMaxLength(30);
        builder.Property(x => x.DefaultAddress);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(10).IsRequired();
        builder.Property(x => x.AvatarUrl);
        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);

        builder.Ignore(x => x.DomainEvents);
    }
}
