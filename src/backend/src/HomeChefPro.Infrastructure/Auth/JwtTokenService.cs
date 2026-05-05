using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HomeChefPro.Application.Auth.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomeChefPro.Infrastructure.Auth;

public sealed class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly TimeProvider _clock = clock;

    public RefreshTokenIssued IssueRefresh()
    {
        // 64 bytes (512 bits) en base64url -> ~86 chars URL-safe.
        var bytes = RandomNumberGenerator.GetBytes(64);
        var plain = Base64UrlEncoder.Encode(bytes);
        var hash = HashRefresh(plain);
        var expiresAt = _clock.GetUtcNow().AddDays(_options.RefreshTokenDays);
        return new RefreshTokenIssued(plain, hash, expiresAt);
    }

    public string HashRefresh(string plain)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plain);
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    public JwtTokenResult Issue(
        Guid userId,
        Guid chefId,
        string email,
        string fullName,
        IReadOnlyCollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
            throw new InvalidOperationException(
                "JWT SigningKey is missing or too short (minimum 32 bytes). Configure 'Jwt:SigningKey'.");

        var now = _clock.GetUtcNow();
        var expires = now.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Name, fullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            // Pasada C / Fase 1C-A: tenant del usuario.
            new("chef_id", chefId.ToString()),
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new JwtTokenResult(jwt, expires);
    }
}
