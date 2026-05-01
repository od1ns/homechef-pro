using HomeChefPro.Domain.Exchange;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RateVesPerUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.EffectiveDate);
        builder.Property(x => x.SetBy).IsRequired();
        builder.Property(x => x.Notes);
        builder.Property(x => x.CreatedAt);

        builder.HasIndex(x => x.EffectiveDate).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}
