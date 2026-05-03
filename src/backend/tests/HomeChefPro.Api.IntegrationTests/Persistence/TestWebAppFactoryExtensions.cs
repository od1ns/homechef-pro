using System.Text;
using HomeChefPro.Infrastructure.Auth;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace HomeChefPro.Api.IntegrationTests.Persistence;

/// <summary>
/// Extensions que sobreescriben servicios registrados por
/// <c>AddInfrastructure</c> + <c>AddAppAuthentication</c> con valores
/// de test, garantizando que tanto el DbContext como las opciones de
/// JWT (generacion + validacion) usen los mismos valores que el test
/// configura via <c>AddInMemoryCollection</c>.
///
/// Por que: el override solo via <c>ConfigureAppConfiguration</c> NO es
/// suficiente porque <c>AddAppAuthentication</c> hace
/// <c>configuration.GetSection(...).Get&lt;JwtOptions&gt;()</c> en TIEMPO
/// DE REGISTRO, capturando los valores de appsettings.json. Despues
/// usa esos valores capturados en las TokenValidationParameters.
/// Resultado: el token se GENERA con la clave del test (via IOptions
/// que es lazy) pero se VALIDA con la clave de appsettings.json.
/// Las dos no coinciden y el JWT bearer rechaza con 401.
///
/// Fix: usar <c>PostConfigure</c> que corre DESPUES de toda la
/// registracion original, sobreescribiendo los valores capturados.
/// </summary>
public static class TestWebAppFactoryExtensions
{
    public static IWebHostBuilder UseTestDatabase(this IWebHostBuilder b, string connectionString)
    {
        b.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HomeChefProDbContext>>();
            services.RemoveAll<DbContextOptions>();

            services.AddDbContext<HomeChefProDbContext>(opts =>
            {
                opts.UseNpgsql(connectionString, npg =>
                {
                    npg.MigrationsHistoryTable("__ef_migrations_history", "public");
                });
            });
        });
        return b;
    }

    /// <summary>
    /// Sobreescribe TANTO los <see cref="JwtOptions"/> (usados al GENERAR
    /// tokens) como los <see cref="JwtBearerOptions"/> (usados al VALIDAR
    /// tokens), garantizando que ambos lados usen los MISMOS valores de
    /// test.
    /// </summary>
    public static IWebHostBuilder UseTestAuth(this IWebHostBuilder b)
    {
        const string testIssuer = "HomeChefPro-Test";
        const string testAudience = "HomeChefPro-Clients-Test";
        var testKey = new string('x', 64);

        b.ConfigureServices(services =>
        {
            // 1) JwtOptions: usado por JwtTokenService al generar el token.
            //    PostConfigure corre DESPUES de Configure, asi gana.
            services.PostConfigure<JwtOptions>(opts =>
            {
                opts.Issuer = testIssuer;
                opts.Audience = testAudience;
                opts.SigningKey = testKey;
                opts.AccessTokenMinutes = 60;
                opts.RefreshTokenDays = 14;
            });

            // 2) JwtBearerOptions: usado por el middleware al validar.
            //    Sobreescribimos TokenValidationParameters con los mismos
            //    valores de test.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.TokenValidationParameters.ValidIssuer = testIssuer;
                    options.TokenValidationParameters.ValidAudience = testAudience;
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testKey));
                });
        });
        return b;
    }
}
