using HomeChefPro.Domain.Invitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeChefPro.Infrastructure.Persistence.Configurations;

public sealed class InvitationCodeUseConfiguration : IEntityTypeConfiguration<InvitationCodeUse>
{
    public void Configure(EntityTypeBuilder<InvitationCodeUse> builder)
    {
        builder.ToTable("invitation_code_uses", t => t.ExcludeFromMigrations());
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InvitationCodeId).IsRequired();
        builder.Property(x => x.UsedByUserId).IsRequired();
        builder.Property(x => x.UsedAt);
        builder.Property(x => x.UserIp).HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasIndex(x => new { x.InvitationCodeId, x.UsedByUserId }).IsUnique();
    }
}
