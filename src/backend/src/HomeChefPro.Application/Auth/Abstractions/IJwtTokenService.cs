namespace HomeChefPro.Application.Auth.Abstractions;

public interface IJwtTokenService
{
    /// <summary>Emite un access token (JWT) firmado.</summary>
    /// <param name="chefId">Pasada C: tenant del usuario. En single-tenant es Chef.PilotoId.</param>
    JwtTokenResult Issue(Guid userId, Guid chefId, string email, string fullName, IReadOnlyCollection<string> roles);

    /// <summary>
    /// Genera un nuevo refresh token (string opaco) y devuelve tanto el token plano
    /// (para entregar al cliente) como el hash SHA-256 (para persistir en la BD).
    /// </summary>
    RefreshTokenIssued IssueRefresh();

    /// <summary>SHA-256 hex del token plano. Idempotente — para verificacion.</summary>
    string HashRefresh(string plain);

    /// <summary>
    /// F-17 (Tier 3): emite un PARTIAL token (TTL 5 min, sin role/chef_id, claim
    /// <c>scope=2fa-pending</c>) que el cliente debe canjear via POST /api/auth/2fa/login
    /// presentando el codigo TOTP.
    /// </summary>
    JwtTokenResult IssuePartialFor2fa(Guid userId);

    /// <summary>
    /// F-17: valida un partial token emitido por <see cref="IssuePartialFor2fa"/>.
    /// Si la firma es valida, no esta expirado, y tiene <c>scope=2fa-pending</c>,
    /// retorna true y el userId; sino retorna false.
    /// </summary>
    bool TryValidatePartial2fa(string token, out Guid userId);
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenIssued(string PlainToken, string TokenHash, DateTimeOffset ExpiresAt);
