using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Invitations;

/// <summary>
/// Audit trail: registro de cada uso de un InvitationCode. Permite reportar
/// que chef genero mas conversiones.
/// </summary>
public sealed class InvitationCodeUse : Entity<Guid>
{
    public Guid InvitationCodeId { get; private set; }
    public Guid UsedByUserId { get; private set; }
    public DateTimeOffset UsedAt { get; private set; }
    public string? UserIp { get; private set; }
    public string? UserAgent { get; private set; }

    private InvitationCodeUse() { }

    private InvitationCodeUse(
        Guid id, Guid invitationCodeId, Guid usedByUserId,
        DateTimeOffset usedAt, string? userIp, string? userAgent)
    {
        Id = id;
        InvitationCodeId = invitationCodeId;
        UsedByUserId = usedByUserId;
        UsedAt = usedAt;
        UserIp = userIp;
        UserAgent = userAgent;
    }

    public static InvitationCodeUse Create(
        Guid invitationCodeId, Guid userId, DateTimeOffset now,
        string? ip, string? userAgent)
    {
        return new InvitationCodeUse(
            id: Guid.NewGuid(),
            invitationCodeId: invitationCodeId,
            usedByUserId: userId,
            usedAt: now,
            userIp: ip is { Length: > 45 } ? ip[..45] : ip,
            userAgent: userAgent is { Length: > 500 } ? userAgent[..500] : userAgent);
    }
}
