namespace HomeChefPro.Application.Auth.Dtos;

/// <summary>
/// Resultado de login. F-17: si <see cref="Requires2fa"/> es true, NO se incluye
/// AccessToken/RefreshToken — el cliente debe llamar POST /api/auth/2fa/login con
/// el <see cref="PartialToken"/> + el codigo TOTP para obtener el JWT real.
/// </summary>
public sealed record AuthResultDto(
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    bool Requires2fa = false,
    string? PartialToken = null,
    DateTimeOffset? PartialExpiresAt = null);

public sealed record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string? DefaultPhone,
    string PreferredLanguage,
    IReadOnlyList<string> Roles);
