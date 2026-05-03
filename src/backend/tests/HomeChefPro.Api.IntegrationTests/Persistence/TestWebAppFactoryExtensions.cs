using HomeChefPro.Application.Abstractions;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HomeChefPro.Api.IntegrationTests.Persistence;

/// <summary>
/// Extension que sobreescribe el DbContext registrado por
/// <c>AddInfrastructure</c> con uno apuntando al contenedor Testcontainers.
///
/// Por que: pasar la connection string solo via
/// <c>ConfigureAppConfiguration</c> + <c>AddInMemoryCollection</c> NO es
/// suficiente porque algun camino (Identity / EF Core) lee la cadena
/// directa desde appsettings.json y termina apuntando a 127.0.0.1:5432
/// (el default). Con <c>ConfigureTestServices</c> reemplazamos los
/// <c>DbContextOptions</c> ya registrados, garantizando que TODA query
/// EF use la BD del fixture.
/// </summary>
public static class TestWebAppFactoryExtensions
{
    public static IWebHostBuilder UseTestDatabase(this IWebHostBuilder b, string connectionString)
    {
        b.ConfigureTestServices(services =>
        {
            // Remover el DbContextOptions registrado por Infrastructure.
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
}
