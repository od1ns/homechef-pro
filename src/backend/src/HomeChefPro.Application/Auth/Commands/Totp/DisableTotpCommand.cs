using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Domain.Common;
using MediatR;

namespace HomeChefPro.Application.Auth.Commands.Totp;

/// <summary>
/// F-17: desactiva 2FA para el usuario actual. Requiere un codigo TOTP valido
/// como prueba de posesion del authenticator (defensa contra "robaron mi
/// session pero no mi authenticator y desactivan 2FA detras de mi").
/// </summary>
public sealed record DisableTotpCommand(string Code) : IRequest<Unit>;

public sealed class DisableTotpValidator : AbstractValidator<DisableTotpCommand>
{
    public DisableTotpValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
    }
}

public sealed class DisableTotpHandler(
    ICurrentUser currentUser,
    ITotpService totp)
    : IRequestHandler<DisableTotpCommand, Unit>
{
    public async Task<Unit> Handle(DisableTotpCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        if (!await totp.IsEnabledAsync(userId, ct).ConfigureAwait(false))
            throw new DomainException("2FA is not enabled for this user.");

        var valid = await totp.VerifyCodeAsync(userId, request.Code, ct).ConfigureAwait(false);
        if (!valid)
            throw new DomainException("Invalid TOTP code.");

        await totp.DisableAsync(userId, ct).ConfigureAwait(false);
        return Unit.Value;
    }
}
