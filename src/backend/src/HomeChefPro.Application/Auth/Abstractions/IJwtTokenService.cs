namespace HomeChefPro.Application.Auth.Abstractions;

public interface IJwtTokenService
{
    JwtTokenResult Issue(Guid userId, string email, string fullName, IReadOnlyCollection<string> roles);
}

public sealed record JwtTokenResult(string AccessToken, DateTimeOffset ExpiresAt);
