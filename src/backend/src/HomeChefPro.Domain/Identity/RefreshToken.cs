using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Identity;

/// <summary>
/// Refresh token persistido. La entidad guarda el HASH (no el token plano),
/// junto con datos de rotacion para reconstruir cadenas si hace falta detectar
/// reuso (token revocado que reaparece -> probable robo).
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedById { get; private set; }
    public string? DeviceInfo { get; private set; }
    public string? IpAddress { get; private set; }

    // EF Core
    private RefreshToken() { }

    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        TimeProvider clock,
        string? deviceInfo = null,
        string? ipAddress = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);
        if (userId == Guid.Empty)
            throw new DomainException("UserId cannot be empty.");
        if (expiresAt <= clock.GetUtcNow())
            throw new DomainException("ExpiresAt must be in the future.");

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = clock.GetUtcNow(),
            DeviceInfo = deviceInfo,
            IpAddress = ipAddress,
        };
    }

    public bool IsActive(TimeProvider clock) =>
        RevokedAt is null && ExpiresAt > clock.GetUtcNow();

    public void Revoke(TimeProvider clock, Guid? replacedById = null)
    {
        if (RevokedAt is not null) return; // idempotente
        RevokedAt = clock.GetUtcNow();
        if (replacedById is not null) ReplacedById = replacedById;
    }
}
