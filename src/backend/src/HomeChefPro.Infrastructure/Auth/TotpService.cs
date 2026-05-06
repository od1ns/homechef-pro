using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Common;
using HomeChefPro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HomeChefPro.Infrastructure.Auth;

/// <summary>
/// F-17: implementacion de <see cref="ITotpService"/> apoyada en
/// <c>UserManager.GetAuthenticatorKeyAsync</c> + Identity TokenStore.
///
/// La libreria de Identity ya implementa todo el algoritmo TOTP estandar
/// (RFC 6238) — solo orquestamos el flujo + construimos el URI otpauth para QR.
/// </summary>
public sealed class TotpService(UserManager<AppUser> users) : ITotpService
{
    private readonly UserManager<AppUser> _users = users;

    public async Task<TotpSetupResult> SetupAsync(Guid userId, string issuer, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("AppUser", userId);

        // Si ya hay key, la reseteamos para evitar reuso.
        var existing = await _users.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        if (string.IsNullOrEmpty(existing))
        {
            await _users.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
            existing = await _users.GetAuthenticatorKeyAsync(user).ConfigureAwait(false);
        }

        if (string.IsNullOrEmpty(existing))
            throw new DomainException("Failed to generate TOTP secret.");

        // Construir el URI otpauth segun spec:
        //   otpauth://totp/{Issuer}:{account}?secret={secret}&issuer={Issuer}
        var account = user.Email ?? user.UserName ?? userId.ToString();
        var encodedIssuer = UrlEncoder.Default.Encode(issuer);
        var encodedAccount = UrlEncoder.Default.Encode(account);
        var uri = $"otpauth://totp/{encodedIssuer}:{encodedAccount}?secret={existing}&issuer={encodedIssuer}&digits=6";

        return new TotpSetupResult(SharedKey: existing, AuthenticatorUri: uri);
    }

    public async Task<bool> VerifySetupAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("AppUser", userId);

        if (await _users.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
            throw new DomainException("2FA already enabled for this user.");

        var clean = NormalizeCode(code);
        var ok = await _users.VerifyTwoFactorTokenAsync(
            user, _users.Options.Tokens.AuthenticatorTokenProvider, clean).ConfigureAwait(false);
        if (!ok) return false;

        var setRes = await _users.SetTwoFactorEnabledAsync(user, true).ConfigureAwait(false);
        return setRes.Succeeded;
    }

    public async Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("AppUser", userId);

        if (!await _users.GetTwoFactorEnabledAsync(user).ConfigureAwait(false))
            return false;

        var clean = NormalizeCode(code);
        return await _users.VerifyTwoFactorTokenAsync(
            user, _users.Options.Tokens.AuthenticatorTokenProvider, clean).ConfigureAwait(false);
    }

    public async Task DisableAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false)
            ?? throw new NotFoundException("AppUser", userId);
        await _users.SetTwoFactorEnabledAsync(user, false).ConfigureAwait(false);
        await _users.ResetAuthenticatorKeyAsync(user).ConfigureAwait(false);
    }

    public async Task<bool> IsEnabledAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;
        return await _users.GetTwoFactorEnabledAsync(user).ConfigureAwait(false);
    }

    private static string NormalizeCode(string code)
    {
        // El usuario puede pegar "123 456" o "123456" — normalizamos.
        var trimmed = (code ?? "").Replace(" ", "").Replace("-", "");
        return trimmed;
    }
}
