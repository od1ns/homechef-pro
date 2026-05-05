using HomeChefPro.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class ChefConfiguration : IEntityTypeConfiguration<Chef>
{
    public void Configure(EntityTypeBuilder<Chef> builder)
    {
        builder.ToTable("chefs", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Rif).HasMaxLength(20).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TradeName).HasMaxLength(120);
        builder.Property(x => x.TaxAddress).IsRequired();

        builder.Property(x => x.Timezone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BaseCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.DisplayCurrency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.InvoicePrefix).HasMaxLength(4).IsRequired();

        builder.Property(x => x.ContactEmail).HasMaxLength(120);
        builder.Property(x => x.ContactPhone).HasMaxLength(30);

        builder.Property(x => x.Status).HasMaxLength(16).IsRequired().HasEnumDbValueConversion();

        builder.Property(x => x.CreatedAt);
        builder.Property(x => x.ActivatedAt);
        builder.Property(x => x.SuspendedAt);
        builder.Property(x => x.ArchivedAt);

        builder.HasIndex(x => x.Rif).IsUnique();

        builder.Ignore(x => x.DomainEvents);
    }
}
