using HomeChefPro.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class InvitationCodeConfiguration : IEntityTypeConfiguration<InvitationCode>
{
    public void Configure(EntityTypeBuilder<InvitationCode> builder)
    {
        builder.ToTable("invitation_codes", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ChefId);
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt);

        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.MaxUses);
        builder.Property(x => x.UsedCount);

        builder.Property(x => x.RevokedAt);
        builder.Property(x => x.RevokedBy);
        builder.Property(x => x.RevocationReason).HasMaxLength(200);

        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Uses)
               .WithOne()
               .HasForeignKey(u => u.InvitationCodeId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
