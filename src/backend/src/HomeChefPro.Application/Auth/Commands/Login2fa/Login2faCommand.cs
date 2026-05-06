using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Auth.Services;
using HomeChefPro.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Auth.Commands.Login2fa;

/// <summary>
/// F-17: segundo paso del login cuando el user tiene 2FA habilitado.
/// El cliente recibio un PartialToken del primer login (paso 1: email/password)
/// + presenta el codigo TOTP del authenticator.
///
/// Flujo:
///   1) Validar partial token (firma, exp, claim scope=2fa-pending).
///   2) Extraer userId del partial.
///   3) Verificar codigo TOTP.
///   4) Emitir JWT real + refresh token.
/// </summary>
public sealed record Login2faCommand(string PartialToken, string Code) : IRequest<AuthResultDto>;

public sealed class Login2faValidator : AbstractValidator<Login2faCommand>
{
    public Login2faValidator()
    {
        RuleFor(x => x.PartialToken).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
    }
}

public sealed class Login2faHandler(
    IHomeChefProDbContext db,
    IIdentityService identity,
    ITotpService totp,
    IJwtTokenService jwt,
    RefreshTokenIssuer refreshIssuer)
    : IRequestHandler<Login2faCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(Login2faCommand request, CancellationToken ct)
    {
        // 1) Validar partial token via abstraccion (no acopla a Infrastructure).
        if (!jwt.TryValidatePartial2fa(request.PartialToken, out var userId))
            throw new UnauthorizedAccessException("Invalid or expired partial token. Re-do login.");

        // 2) Verificar codigo TOTP.
        var totpOk = await totp.VerifyCodeAsync(userId, request.Code, ct).ConfigureAwait(false);
        if (!totpOk)
            throw new UnauthorizedAccessException("Invalid TOTP code.");

        // 3) Emitir JWT real + refresh.
        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(HomeChefPro.Domain.Identity.UserProfile), userId);

        var roles = await identity.GetRolesAsync(userId, ct).ConfigureAwait(false);
        var email = await identity.GetEmailAsync(userId, ct).ConfigureAwait(false)
                    ?? throw new UnauthorizedAccessException("User has no email on record.");

        var token = jwt.Issue(
            userId: userId,
            chefId: HomeChefPro.Domain.Tenancy.Chef.PilotoId,
            email: email,
            fullName: profile.FullName,
            roles: roles);
        var refresh = await refreshIssuer.IssueAndPersistAsync(userId, ct: ct).ConfigureAwait(false);

        return new AuthResultDto(
            UserId: userId,
            Email: email,
            FullName: profile.FullName,
            Roles: [.. roles],
            AccessToken: token.AccessToken,
            ExpiresAt: token.ExpiresAt,
            RefreshToken: refresh.PlainToken,
            RefreshExpiresAt: refresh.ExpiresAt);
    }
}
