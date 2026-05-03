using System.IO;
using DotNet.Testcontainers.Builders;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace HomeChefPro.Api.IntegrationTests.Persistence;

/// <summary>
/// Spins up a real PostgreSQL 16 container, applies all SQL schema + seed scripts
/// from <c>src/database</c>, and exposes a ready-to-use <see cref="HomeChefProDbContext"/>.
/// Requires Docker Desktop running.
/// </summary>
public sealed class LiveDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("homechef")
        .WithUsername("homechef")
        .WithPassword("homechef_test")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public HomeChefProDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HomeChefProDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        return new HomeChefProDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ApplyScriptsAsync("schema");
        await ApplyScriptsAsync("seed");
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private async Task ApplyScriptsAsync(string folder)
    {
        var root = FindRepoRoot();
        var scriptsDir = Path.Combine(root, "src", "database", folder);
        if (!Directory.Exists(scriptsDir))
            throw new DirectoryNotFoundException(
                $"SQL scripts directory not found: {scriptsDir}");

        var scripts = Directory.GetFiles(scriptsDir, "*.sql")
            .Where(p => !Path.GetFileName(p).StartsWith("99_", StringComparison.Ordinal))
            .OrderBy(p => p, StringComparer.Ordinal);

        // Usamos ADO.NET directo en vez de EF.ExecuteSqlRawAsync porque este
        // ultimo parsea el SQL como format string buscando placeholders {N}.
        // Los archivos del schema tienen `{` literales (ej: 13_customer_prefs
        // con `'JSON: { ... }'`) que disparan System.FormatException.
        await using var conn = new Npgsql.NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        foreach (var path in scripts)
        {
            var sql = await File.ReadAllTextAsync(path);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "database")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException(
            "Unable to locate repo root (looked for 'src/database' ancestor).");
    }
}
