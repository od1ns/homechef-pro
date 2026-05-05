using HomeChefPro.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class PaymentProofUploadConfiguration : IEntityTypeConfiguration<PaymentProofUpload>
{
    public void Configure(EntityTypeBuilder<PaymentProofUpload> builder)
    {
        builder.ToTable("payment_proof_uploads", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        // Pasada C / Fase 1C-A: tenant root. Sentinel Guid.Empty + SQL DEFAULT
        // hace que codigo single-tenant siga funcionando sin pasar ChefId.
        builder.Property(x => x.ChefId)
               .HasColumnName("chef_id")
               .HasDefaultValueSql("'00000000-0000-0000-0000-000000000001'::uuid")
               .HasSentinel(Guid.Empty);

        builder.Property(x => x.Filename).HasMaxLength(96).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.ClaimedAt);
        builder.Property(x => x.ClaimedByPaymentId);

        builder.HasIndex(x => x.Filename).IsUnique();
    }
}
