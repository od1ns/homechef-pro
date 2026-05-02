namespace HomeChefPro.Application.Auth.Abstractions;

public interface IJwtTokenService
{
    /// <summary>Emite un access token (JWT) firmado.</summary>
    JwtTokenResult Issue(Guid userId, string email, string fullName, IReadOnlyCollection<string> roles);

    /// <summary>
    /// Genera un nuevo refresh token (string opaco) y devuelve tanto el token plano
    /// (para entregar al cliente) como el hash SHA-256 (para persistir en la BD).
    /// </summary>
    RefreshTokenIssued IssueRefresh();

    /// <summary>SHA-256 hex del token plano. Idempotente — para verificacion.</summary>
    string HashRefresh(string plain);
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);

public sealed record RefreshTokenIssued(string PlainToken, string TokenHash, DateTimeOffset ExpiresAt);
