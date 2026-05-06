using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Common;
using MediatR;

namespace HomeChefPro.Application.Auth.Commands.Totp;

/// <summary>
/// F-17: valida el primer codigo TOTP del usuario y, si es correcto, marca
/// al user como TwoFactorEnabled=true. A partir de ese momento, los logins
/// devuelven partial token + Requires2fa=true en lugar del JWT real.
/// </summary>
public sealed record VerifyTotpSetupCommand(string Code) : IRequest<Unit>;

public sealed class VerifyTotpSetupValidator : AbstractValidator<VerifyTotpSetupCommand>
{
    public VerifyTotpSetupValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
    }
}

public sealed class VerifyTotpSetupHandler(
    ICurrentUser currentUser,
    ITotpService totp)
    : IRequestHandler<VerifyTotpSetupCommand, Unit>
{
    public async Task<Unit> Handle(VerifyTotpSetupCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var ok = await totp.VerifySetupAsync(userId, request.Code, ct).ConfigureAwait(false);
        if (!ok)
            throw new DomainException("Invalid TOTP code. Verify the time on your device and the secret in the authenticator app.");
        return Unit.Value;
    }
}
