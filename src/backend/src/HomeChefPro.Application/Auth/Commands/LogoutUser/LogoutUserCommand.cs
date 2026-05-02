using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Auth.Commands.LogoutUser;

/// <summary>
/// Revoca el refresh token actual. El access token (JWT) sigue valido hasta
/// su expiracion natural — para revocar ESO hace falta una blacklist por jti
/// o reducir AccessTokenMinutes. Por ahora aceptamos esa ventana corta.
/// </summary>
public sealed record LogoutUserCommand(string RefreshToken) : IRequest;

public sealed class LogoutUserValidator : AbstractValidator<LogoutUserCommand>
{
    public LogoutUserValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(200);
    }
}

public sealed class LogoutUserHandler(
    IHomeChefProDbContext db,
    IJwtTokenService jwt,
    TimeProvider clock)
    : IRequestHandler<LogoutUserCommand>
{
    public async Task Handle(LogoutUserCommand request, CancellationToken ct)
    {
        var hash = jwt.HashRefresh(request.RefreshToken);
        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            .ConfigureAwait(false);
        // Idempotente: si no existe o ya esta revocado, no hacemos nada y
        // devolvemos exito (el cliente ya esta deslogueado).
        if (stored is null) return;
        stored.Revoke(clock);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
