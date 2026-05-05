using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Auth.Commands.RefreshAccessToken;

/// <summary>
/// Intercambia un refresh token activo por un nuevo par (access + refresh).
/// El refresh viejo se revoca y queda apuntando al nuevo via ReplacedById,
/// asi se puede detectar reuso (intento de usar uno revocado -> probable robo
/// -> revocamos toda la cadena del usuario).
/// </summary>
public sealed record RefreshAccessTokenCommand(string RefreshToken)
    : IRequest<AuthResultDto>;

public sealed class RefreshAccessTokenValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}

public sealed class RefreshAccessTokenHandler(
    IHomeChefProDbContext db,
    IIdentityService identity,
    IJwtTokenService jwt,
    TimeProvider clock)
    : IRequestHandler<RefreshAccessTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshAccessTokenCommand request, CancellationToken ct)
    {
        var hash = jwt.HashRefresh(request.RefreshToken);

        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            .ConfigureAwait(false);

        if (stored is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        // Detección de reuso: si llega un token revocado, asumimos robo y
        // revocamos toda la cadena de ese usuario.
        if (stored.RevokedAt is not null)
        {
            await RevokeAllForUserAsync(stored.UserId, ct).ConfigureAwait(false);
            throw new UnauthorizedAccessException(
                "Refresh token reuse detected. All sessions have been revoked.");
        }

        if (!stored.IsActive(clock))
            throw new UnauthorizedAccessException("Refresh token expired.");

        // Cargar info del usuario para emitir el nuevo access token con claims.
        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == stored.UserId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(
                nameof(HomeChefPro.Domain.Identity.UserProfile),
                stored.UserId);

        var roles = await identity.GetRolesAsync(stored.UserId, ct).ConfigureAwait(false);
        var email = await identity.GetEmailAsync(stored.UserId, ct).ConfigureAwait(false);
        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException("User has no email on record.");

        // Rotacion: insertamos el nuevo token PRIMERO (SaveChanges) y despues
        // revocamos el viejo apuntandolo. Si los dos cambios viajan en la
        // misma transaccion, EF no garantiza el orden y el FK
        // refresh_tokens_replaced_by_id_fkey puede violarse.
        var newRefresh = jwt.IssueRefresh();
        var newEntity = HomeChefPro.Domain.Identity.RefreshToken.Issue(
            userId: stored.UserId,
            tokenHash: newRefresh.TokenHash,
            expiresAt: newRefresh.ExpiresAt,
            clock: clock);
        db.RefreshTokens.Add(newEntity);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        stored.Revoke(clock, replacedById: newEntity.Id);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Pasada C / Fase 1C-A: re-emit con el mismo chef del piloto.
        var token = jwt.Issue(
            userId: stored.UserId,
            chefId: HomeChefPro.Domain.Tenancy.Chef.PilotoId,
            email: email,
            fullName: profile.FullName,
            roles: roles);

        return new AuthResultDto(
            UserId: stored.UserId,
            Email: email,
            FullName: profile.FullName,
            Roles: [.. roles],
            AccessToken: token.AccessToken,
            ExpiresAt: token.ExpiresAt,
            RefreshToken: newRefresh.PlainToken,
            RefreshExpiresAt: newRefresh.ExpiresAt);
    }

    private async Task RevokeAllForUserAsync(Guid userId, CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var t in active)
            t.Revoke(clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
