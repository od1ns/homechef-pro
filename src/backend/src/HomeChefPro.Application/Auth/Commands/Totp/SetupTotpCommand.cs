using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using MediatR;

namespace HomeChefPro.Application.Auth.Commands.Totp;

/// <summary>
/// F-17: inicia el setup de 2FA para el usuario actual. Retorna el secret
/// (debug) y el URI otpauth que el cliente convierte en QR. El usuario
/// debe escanear el QR con Authy/Google Authenticator y luego llamar
/// VerifyTotpSetup con el primer codigo para activar 2FA.
/// </summary>
public sealed record SetupTotpCommand : IRequest<TotpSetupResult>;

public sealed class SetupTotpHandler(
    ICurrentUser currentUser,
    ITotpService totp)
    : IRequestHandler<SetupTotpCommand, TotpSetupResult>
{
    public Task<TotpSetupResult> Handle(SetupTotpCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        return totp.SetupAsync(userId, issuer: "HomeChef Pro", ct: ct);
    }
}
