using HomeChefPro.Domain.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class DeliveryEventConfiguration : IEntityTypeConfiguration<DeliveryEvent>
{
    public void Configure(EntityTypeBuilder<DeliveryEvent> builder)
    {
        builder.ToTable("delivery_events", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(60).IsRequired();
        builder.Property(x => x.ExternalTrackingId).HasMaxLength(120);
        builder.Property(x => x.NormalizedStatus).HasMaxLength(24).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.RawStatus).HasMaxLength(60);
        builder.Property(x => x.RawPayloadJson).HasColumnName("raw_payload").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.Signature);
        builder.Property(x => x.SignatureValid);
        builder.Property(x => x.ReceivedAt);
    }
}
