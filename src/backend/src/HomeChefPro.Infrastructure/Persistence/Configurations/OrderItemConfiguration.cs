using HomeChefPro.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.DishId).IsRequired();
        builder.Property(x => x.DishNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UnitPriceUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.Quantity);
        builder.Property(x => x.LineTotalUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.ItemNotes);
        builder.Property(x => x.KitchenStatus).HasMaxLength(16).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.PrepStartedAt);
        builder.Property(x => x.PrepCompletedAt);

        builder.Ignore(x => x.IsReady);
    }
}
