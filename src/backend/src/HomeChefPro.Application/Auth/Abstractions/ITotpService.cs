namespace HomeChefPro.Application.Auth.Abstractions;

/// <summary>
/// F-17 (Tier 3): MFA TOTP. Esta abstraccion vive en Application para no
/// acoplar a UserManager/Identity. Infrastructure provee la implementacion
/// que usa <c>UserManager.GetAuthenticatorKeyAsync</c> + Identity TokenStore.
/// </summary>
public interface ITotpService
{
    /// <summary>
    /// Genera (o recupera) el secret TOTP del usuario y retorna el URI
    /// otpauth://... que el cliente puede convertir en QR. NO marca al user
    /// como TwoFactorEnabled — eso se hace tras VerifySetupAsync.
    /// </summary>
    Task<TotpSetupResult> SetupAsync(Guid userId, string issuer, CancellationToken ct = default);

    /// <summary>
    /// Verifica el primer codigo TOTP del usuario y, si es valido, marca al
    /// user como <c>TwoFactorEnabled = true</c>. Idempotente: si ya estaba
    /// habilitado, falla con error explicativo.
    /// </summary>
    Task<bool> VerifySetupAsync(Guid userId, string code, CancellationToken ct = default);

    /// <summary>
    /// Verifica un codigo TOTP de un user que YA tiene 2FA habilitado.
    /// Retorna true si el codigo es valido en la ventana actual o adyacente.
    /// </summary>
    Task<bool> VerifyCodeAsync(Guid userId, string code, CancellationToken ct = default);

    /// <summary>
    /// Deshabilita 2FA. Resetea el secret y marca <c>TwoFactorEnabled = false</c>.
    /// Requiere validacion previa de credenciales por el caller.
    /// </summary>
    Task DisableAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// True si el user tiene 2FA habilitado.
    /// </summary>
    Task<bool> IsEnabledAsync(Guid userId, CancellationToken ct = default);
}

/// <summary>
/// Resultado de <see cref="ITotpService.SetupAsync"/>: secret en base32 (para
/// debugging si es necesario) + URI otpauth listo para QR.
/// </summary>
public sealed record TotpSetupResult(string SharedKey, string AuthenticatorUri);
