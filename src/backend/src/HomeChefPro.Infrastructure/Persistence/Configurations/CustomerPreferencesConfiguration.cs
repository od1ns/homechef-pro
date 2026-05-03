using HomeChefPro.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class CustomerPreferencesConfiguration : IEntityTypeConfiguration<CustomerPreferences>
{
    public void Configure(EntityTypeBuilder<CustomerPreferences> builder)
    {
        builder.ToTable("customer_preferences", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("user_id");

        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.Ignore(x => x.DomainEvents);
    }
}
