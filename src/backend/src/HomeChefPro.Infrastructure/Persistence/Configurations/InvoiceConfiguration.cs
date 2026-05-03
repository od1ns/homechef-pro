using HomeChefPro.Domain.Invoicing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId).IsRequired();

        builder.Property(x => x.SubtotalUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.IvaUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.IgtfUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.TotalWithTaxUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.IvaRate).HasColumnType("numeric(6,4)");
        builder.Property(x => x.IgtfRate).HasColumnType("numeric(6,4)");
        builder.Property(x => x.IgtfApplies);

        builder.Property(x => x.IssuerRif).HasMaxLength(20);
        builder.Property(x => x.IssuerLegalName).HasMaxLength(200);
        builder.Property(x => x.IssuerAddress);
        builder.Property(x => x.CustomerRif).HasMaxLength(20);
        builder.Property(x => x.CustomerLegalName).HasMaxLength(200);
        builder.Property(x => x.CustomerAddress);

        builder.Property(x => x.Provider).HasMaxLength(40).IsRequired();
        builder.Property(x => x.FiscalNumber).HasMaxLength(40);
        builder.Property(x => x.ControlNumber).HasMaxLength(40);
        builder.Property(x => x.ProviderResponseJson).HasColumnType("jsonb");

        builder.Property(x => x.Status).HasMaxLength(20).IsRequired().HasEnumDbValueConversion();

        builder.Property(x => x.IssuedAt);
        builder.Property(x => x.CancelledAt);
        builder.Property(x => x.CancellationReason);
        builder.Property(x => x.IssuedBy);

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.UpdatedAt);
        builder.Property(x => x.UpdatedAt).Metadata.SetAfterSaveBehavior(Microsoft.EntityFrameworkCore.Metadata.PropertySaveBehavior.Ignore);

        builder.HasIndex(x => x.OrderId).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
