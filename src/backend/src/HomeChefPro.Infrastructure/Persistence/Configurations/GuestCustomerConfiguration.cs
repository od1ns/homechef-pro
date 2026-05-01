using HomeChefPro.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class GuestCustomerConfiguration : IEntityTypeConfiguration<GuestCustomer>
{
    public void Configure(EntityTypeBuilder<GuestCustomer> builder)
    {
        builder.ToTable("guest_customers", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        builder.Property(x => x.CreatedAt);

        builder.Ignore(x => x.DomainEvents);
    }
}
