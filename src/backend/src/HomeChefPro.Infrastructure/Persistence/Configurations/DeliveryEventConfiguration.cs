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

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

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
