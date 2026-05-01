using HomeChefPro.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class DeliveryTrackingConfiguration : IEntityTypeConfiguration<DeliveryTracking>
{
    public void Configure(EntityTypeBuilder<DeliveryTracking> builder)
    {
        builder.ToTable("delivery_tracking", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ExternalTrackingId).HasMaxLength(120);
        builder.Property(x => x.CurrentStatus).HasMaxLength(24).IsRequired().HasEnumDbValueConversion();

        builder.Property(x => x.CourierName).HasMaxLength(120);
        builder.Property(x => x.CourierPhone).HasMaxLength(30);
        builder.Property(x => x.CourierVehicle).HasMaxLength(60);

        builder.Property(x => x.LastKnownLat).HasColumnType("numeric(9,6)");
        builder.Property(x => x.LastKnownLng).HasColumnType("numeric(9,6)");
        builder.Property(x => x.LastEventAt);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => x.OrderId).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
