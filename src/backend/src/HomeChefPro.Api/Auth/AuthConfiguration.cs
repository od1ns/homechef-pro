using System.Text;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace HomeChefPro.Api.Auth;

public static class AuthConfiguration
{
    public static IServiceCollection AddAppAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");

        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
            throw new InvalidOperationException(
                "Jwt:SigningKey must be at least 32 characters. Set it in user-secrets or environment.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
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
