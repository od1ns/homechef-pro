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

        builder.Property(x => x.Filename).HasMaxLength(96).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.UploadedAt).IsRequired();
        builder.Property(x => x.ClaimedAt);
        builder.Property(x => x.ClaimedByPaymentId);

        builder.HasIndex(x => x.Filename).IsUnique();
    }
}
