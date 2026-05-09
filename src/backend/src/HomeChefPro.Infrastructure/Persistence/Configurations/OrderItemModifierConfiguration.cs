using HomeChefPro.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
{
    public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
    {
        builder.ToTable("order_item_modifiers", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderItemId).IsRequired();
        builder.Property(x => x.ModifierId).IsRequired();
        builder.Property(x => x.ModifierNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.PriceDeltaUsdSnapshot).HasColumnType("numeric(12,4)").IsRequired();
        builder.Property(x => x.LineDeltaUsd).HasColumnType("numeric(12,4)").IsRequired();
    }
}
