using System.Security.Cryptography;
using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Invitations;

/// <summary>
/// Codigo de invitacion para restringir registro publico.
/// Sesion A / Frente 1. Modelo C: chef_id NULL = global SaaS; NOT NULL = rastrea chef.
/// </summary>
public sealed class InvitationCode : AggregateRoot<Guid>
{
    private readonly List<InvitationCodeUse> _uses = [];

    public string Code { get; private set; } = null!;
    public Guid? ChefId { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ExpiresAt { get; private set; }
    public int MaxUses { get; private set; }
    public int UsedCount { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? RevokedBy { get; private set; }
    public string? RevocationReason { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<InvitationCodeUse> Uses => _uses.AsReadOnly();

    private InvitationCode() { }

    private InvitationCode(
        Guid id,
        string code,
        Guid? chefId,
        Guid createdBy,
        DateTimeOffset? expiresAt,
        int maxUses,
        string? notes,
        DateTimeOffset now)
    {
        Id = id;
        Code = code;
        ChefId = chefId;
        CreatedBy = createdBy;
        CreatedAt = now;
        ExpiresAt = expiresAt;
        MaxUses = maxUses;
        UsedCount = 0;
        Notes = notes;
    }

    public static InvitationCode Create(
        Guid createdBy,
        DateTimeOffset now,
        Guid? chefId = null,
        DateTimeOffset? expiresAt = null,
        int maxUses = 1,
        string? notes = null,
        string? customCode = null)
    {
        if (createdBy == Guid.Empty)
            throw new DomainException("createdBy is required.");
        if (maxUses < 1)
            throw new DomainException("maxUses must be >= 1.");
        if (expiresAt is not null && expiresAt <= now)
            throw new DomainException("expiresAt must be in the future.");
        if (notes is { Length: > 500 })
            throw new DomainException("notes max length is 500.");

        var code = string.IsNullOrWhiteSpace(customCode)
            ? GenerateRandomCode()
            : customCode.Trim().ToUpperInvariant();

        return new InvitationCode(
            id: Guid.NewGuid(),
            code: code,
            chefId: chefId,
            createdBy: createdBy,
            expiresAt: expiresAt,
            maxUses: maxUses,
            notes: notes,
            now: now);
    }

    /// <summary>
    /// Genera 12 chars alfa-num sin caracteres confusos (0, O, I, 1, l).
    /// ~5x10^17 combinaciones posibles, suficiente entropia.
    /// </summary>
    private static string GenerateRandomCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        Span<char> buffer = stackalloc char[12];
        Span<byte> bytes = stackalloc byte[12];
        RandomNumberGenerator.Fill(bytes);
        for (int i = 0; i < 12; i++)
            buffer[i] = alphabet[bytes[i] % alphabet.Length];
        return new string(buffer);
    }

    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null
        && (ExpiresAt is null || ExpiresAt > now)
        && UsedCount < MaxUses;

    /// <summary>
    /// Intenta consumir una unidad del codigo. Si IsActive, incrementa UsedCount
    /// y registra el uso. Retorna true si consumio, false si no podia.
    /// El llamador es responsable de SaveChangesAsync — el incremento es local.
    /// </summary>
    public bool TryUse(Guid userId, DateTimeOffset now, string? ip = null, string? userAgent = null)
    {
        if (!IsActive(now)) return false;
        if (_uses.Any(u => u.UsedByUserId == userId))
            return false; // mismo user no puede usar el mismo codigo dos veces

        UsedCount++;
        _uses.Add(InvitationCodeUse.Create(Id, userId, now, ip, userAgent));
        return true;
    }

    public void Revoke(Guid byUserId, DateTimeOffset now, string? reason = null)
    {
        if (RevokedAt is not null)
            throw new DomainException("Code is already revoked.");
        RevokedAt = now;
        RevokedBy = byUserId;
        RevocationReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }
}
