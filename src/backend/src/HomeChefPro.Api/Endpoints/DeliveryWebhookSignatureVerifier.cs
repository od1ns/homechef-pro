using System.Security.Cryptography;
using System.Text;

namespace HomeChefPro.Api.Endpoints;

/// <summary>
/// Verifica HMAC-SHA256 del cuerpo del webhook contra un secret por proveedor.
/// El secret se configura en <c>DeliveryWebhooks:Secrets:{providerName}</c>
/// (case-insensitive). Si no hay secret configurado para el provider, la
/// verificacion devuelve <c>null</c> (no aplica) y el evento se ingesta como
/// "no verificado" — util mientras se conectan los secrets reales.
/// </summary>
public sealed class DeliveryWebhookSignatureVerifier
{
    private readonly Dictionary<string, byte[]> _secrets;

    public DeliveryWebhookSignatureVerifier(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection("DeliveryWebhooks:Secrets");
        _secrets = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in section.GetChildren())
        {
            if (string.IsNullOrWhiteSpace(pair.Value)) continue;
            _secrets[pair.Key] = Encoding.UTF8.GetBytes(pair.Value);
        }
    }

    /// <summary>
    /// Devuelve <c>true</c> si la firma coincide con HMAC-SHA256 del body,
    /// <c>false</c> si hay secret configurado pero la firma no coincide,
    /// <c>null</c> si no hay secret para este provider (no se valida).
    /// </summary>
    /// <remarks>
    /// Acepta firmas en hex o base64. Tolerante a un prefijo
    /// <c>"sha256="</c> tipico en GitHub-style webhooks.
    /// </remarks>
    public bool? Verify(string provider, string? signatureHeader, string rawBody)
    {
        if (!_secrets.TryGetValue(provider, out var secret)) return null;
        if (string.IsNullOrWhiteSpace(signatureHeader)) return false;

        var sig = signatureHeader.Trim();
        if (sig.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            sig = sig["sha256=".Length..];

        using var hmac = new HMACSHA256(secret);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody ?? string.Empty);
        var computed = hmac.ComputeHash(bodyBytes);

        // Try hex first, then base64.
        if (TryDecodeHex(sig, out var provided) && provided.Length == computed.Length)
            return CryptographicOperations.FixedTimeEquals(provided, computed);

        if (TryDecodeBase64(sig, out provided) && provided.Length == computed.Length)
            return CryptographicOperations.FixedTimeEquals(provided, computed);

        return false;
    }

    private static bool TryDecodeHex(string s, out byte[] bytes)
    {
        try
        {
            bytes = Convert.FromHexString(s);
            return true;
        }
        catch (FormatException)
        {
            bytes = Array.Empty<byte>();
            return false;
        }
    }

    private static bool TryDecodeBase64(string s, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        try
        {
            bytes = Convert.FromBase64String(s);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
