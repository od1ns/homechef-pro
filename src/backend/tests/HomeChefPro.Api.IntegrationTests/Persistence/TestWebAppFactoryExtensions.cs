using HomeChefPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
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
/// (el default). Con <c>ConfigureServices</c> aplicado despues del
/// registro original, removemos los <c>DbContextOptions</c> ya
/// registrados y reinsertamos uno apuntando a la BD del fixture.
///
/// Nota: <c>ConfigureServices</c> en <c>IWebHostBuilder</c> se ejecuta
/// en orden. Como el test lo agrega via <c>WithWebHostBuilder</c>
/// DESPUES de los <c>builder.Services.AddXxx</c> de Program.cs, gana.
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
}
