namespace HomeChefPro.Application.Auth.Dtos;

public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTimeOffset ExpiresAt);

public sealed record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string? DefaultPhone,
    string PreferredLanguage,
    IReadOnlyList<string> Roles);
