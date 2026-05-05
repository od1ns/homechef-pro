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

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

        builder.Property(x => x.RateVesPerUsd).HasColumnType("numeric(14,4)");
        builder.Property(x => x.EffectiveDate);
        builder.Property(x => x.SetBy).IsRequired();
        builder.Property(x => x.Notes);
        builder.Property(x => x.CreatedAt);

        // Pasada C / H-02: UNIQUE compuesto con chef_id.
        builder.HasIndex(x => new { x.ChefId, x.EffectiveDate }).IsUnique();
        builder.Ignore(x => x.DomainEvents);
    }
}
