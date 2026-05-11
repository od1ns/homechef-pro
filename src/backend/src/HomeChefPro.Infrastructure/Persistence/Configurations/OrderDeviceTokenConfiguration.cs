using HomeChefPro.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class OrderDeviceTokenConfiguration : IEntityTypeConfiguration<OrderDeviceToken>
{
    public void Configure(EntityTypeBuilder<OrderDeviceToken> builder)
    {
        builder.ToTable("order_device_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.FcmToken).HasColumnName("fcm_token").HasMaxLength(4096);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(x => x.OrderId).IsUnique();
    }
}
