using System.Text;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HomeChefPro.Api.Auth;

public static class AuthConfiguration
{
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // F-03 (audit Pasada A): bind JwtOptions desde IConfiguration y validar al startup
        // via IValidateOptions, que corre DESPUES de cualquier PostConfigure inyectado por
        // tests (UseTestAuth) o por overrides de configuracion (AddInMemoryCollection).
        // ValidateOnStart() fuerza que la validacion corra al construir el host, fail-fast.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configurar JwtBearerOptions desde IOptions<JwtOptions> de forma "lazy".
        // El callback se ejecuta cuando el sistema resuelve JwtBearerOptions (despues de
        // PostConfigure), por lo que tests pueden sobrescribir la JwtOptions y aqui
        // recibimos el valor final.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOpts) =>
            {
                var jwt = jwtOpts.Value;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                    ClockSkew = TimeSpan.FromMinutes(1),
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin",   p => p.RequireRole(Roles.Admin));
            options.AddPolicy("Cashier", p => p.RequireRole(Roles.Cashier, Roles.Admin));
            options.AddPolicy("Cook",    p => p.RequireRole(Roles.Cook, Roles.Admin));
            options.AddPolicy("Client",  p => p.RequireRole(Roles.Client, Roles.Admin));
        });

        return services;
    }
}

/// <summary>
/// Valida <see cref="JwtOptions"/> al startup con mensajes especificos por tipo de fallo
/// (vacio, demasiado corto, placeholder conocido).
///
/// Se registra como <see cref="IValidateOptions{TOptions}"/> + <c>ValidateOnStart()</c> de modo
/// que se evalua despues de los PostConfigure (incluidos los inyectados por test fixtures
/// como <c>UseTestAuth</c>), lo que evita falsos negativos cuando el valor real proviene de
/// <c>AddInMemoryCollection</c> o user-secrets en lugar de <c>appsettings.json</c>.
/// </summary>
internal sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("JwtOptions is null.");

        if (string.IsNullOrWhiteSpace(options.SigningKey))
        {
            return ValidateOptionsResult.Fail(
                "Jwt:SigningKey is required. Set the env var JWT_SIGNING_KEY (or user-secrets in dev). " +
                "Generate one with: openssl rand -base64 48");
        }

        if (options.SigningKey.Length < 32)
        {
            return ValidateOptionsResult.Fail(
                "Jwt:SigningKey must be at least 32 characters. Generate one with: openssl rand -base64 48");
        }

        var lower = options.SigningKey.ToLowerInvariant();
        if (lower.StartsWith("reemplazar", StringComparison.Ordinal)
            || lower.StartsWith("change-me", StringComparison.Ordinal)
            || lower.StartsWith("changeme", StringComparison.Ordinal)
            || lower.Contains("placeholder", StringComparison.Ordinal)
            || lower.Contains("example", StringComparison.Ordinal))
        {
            return ValidateOptionsResult.Fail(
                "Jwt:SigningKey looks like a placeholder/default value. " +
                "Generate a real secret with: openssl rand -base64 48 and inject it via JWT_SIGNING_KEY env var.");
        }

        return ValidateOptionsResult.Success;
    }
}
