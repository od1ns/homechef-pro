using HomeChefPro.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.Method).HasMaxLength(24).IsRequired().HasEnumDbValueConversion();

        builder.Property(x => x.AmountUsd).HasColumnType("numeric(12,4)");
        builder.Property(x => x.PaidCurrency).HasColumnType("char(3)").IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.AmountPaidCurrency).HasColumnType("numeric(16,2)");
        builder.Property(x => x.ExchangeRateUsed).HasColumnType("numeric(14,4)");

        builder.Property(x => x.ReferenceNumber).HasMaxLength(80);
        builder.Property(x => x.ProofImageUrl);
        builder.Property(x => x.PayerName).HasMaxLength(160);
        builder.Property(x => x.PayerPhone).HasMaxLength(30);
        builder.Property(x => x.PayerAccountLast4).HasMaxLength(10);

        builder.Property(x => x.Status).HasMaxLength(16).IsRequired().HasEnumDbValueConversion();
        builder.Property(x => x.VerifiedBy);
        builder.Property(x => x.VerifiedAt);
        builder.Property(x => x.RejectionReason);

        builder.Property(x => x.CreatedAt);

        builder.Ignore(x => x.DomainEvents);
    }
}
