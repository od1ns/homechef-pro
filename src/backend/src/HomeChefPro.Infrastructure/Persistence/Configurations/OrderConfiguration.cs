using HomeChefPro.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderNumber).HasMaxLength(24)
               .ValueGeneratedOnAdd()
               .Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.Property(x => x.CustomerType).HasMaxLength(20).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.UserId);
        builder.Property(x => x.GuestCustomerId);

        builder.Property(x => x.Status).HasMaxLength(24).IsRequired().HasEnumDbValueConversion();

        builder.Property(x => x.DeliveryType).HasMaxLength(20).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.DeliveryAddress);
        builder.Property(x => x.DeliveryInstructions);
        builder.Property(x => x.ContactPhone).HasMaxLength(30);

        builder.Property(x => x.ScheduledFor);
        builder.Property(x => x.PrepEstimatedReadyAt);

        builder.Property(x => x.CustomerNotes);

        builder.Property(x => x.SubtotalUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.DiscountUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.DeliveryFeeUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.TotalUsd).HasColumnType("numeric(12,4)");

        builder.Property(x => x.ExchangeRateId);
        builder.Property(x => x.RateVesPerUsdAtOrder).HasColumnType("numeric(14,4)");
        builder.Property(x => x.TotalVesAtOrderTime).HasColumnType("numeric(16,2)");

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.PaidAt);
        builder.Property(x => x.PrepStartedAt);
        builder.Property(x => x.ReadyAt);
        builder.Property(x => x.DeliveredAt);
        builder.Property(x => x.CancelledAt);
        builder.Property(x => x.CancellationReason);

        builder.HasIndex(x => x.OrderNumber).IsUnique();

        builder.HasMany(x => x.Items)
               .WithOne()
               .HasForeignKey(i => i.OrderId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.AllItemsReady);
    }
}
