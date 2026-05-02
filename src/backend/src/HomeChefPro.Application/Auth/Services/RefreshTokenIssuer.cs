using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Domain.Identity;

namespace HomeChefPro.Application.Auth.Services;

/// <summary>
/// Encapsula la creacion + persistencia de un refresh token. Login, Register
/// y RefreshToken usan este helper para no duplicar la logica de hashing y
/// rotacion.
/// </summary>
public sealed class RefreshTokenIssuer(
    IHomeChefProDbContext db,
    IJwtTokenService jwt,
    TimeProvider clock)
{
    /// <summary>
    /// Emite un nuevo refresh token, lo persiste con el hash, y devuelve la
    /// version plana al caller para devolverla al cliente.
    /// </summary>
    public async Task<RefreshTokenIssued> IssueAndPersistAsync(
        Guid userId,
        string? deviceInfo = null,
        string? ipAddress = null,
        CancellationToken ct = default)
    {
        var issued = jwt.IssueRefresh();
        var entity = RefreshToken.Issue(
            userId: userId,
            tokenHash: issued.TokenHash,
            expiresAt: issued.ExpiresAt,
            clock: clock,
            deviceInfo: deviceInfo,
            ipAddress: ipAddress);
        db.RefreshTokens.Add(entity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return issued;
    }
}
