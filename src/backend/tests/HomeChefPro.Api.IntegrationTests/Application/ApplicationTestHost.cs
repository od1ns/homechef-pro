using HomeChefPro.Application;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Api.IntegrationTests.Persistence;
using HomeChefPro.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HomeChefPro.Api.IntegrationTests.Application;

/// <summary>
/// Boots the Application + a live DbContext against the Testcontainers fixture. Provides a
/// scoped <see cref="IMediator"/> so integration tests can dispatch commands/queries.
/// </summary>
public sealed class ApplicationTestHost : IAsyncDisposable
{
    private readonly ServiceProvider _root;
    public IServiceScope Scope { get; }

    public ApplicationTestHost(LiveDatabaseFixture fixture, Guid? currentUserId = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddProvider(NullLoggerProvider.Instance));
        services.AddApplication();
        services.AddDbContext<HomeChefProDbContext>(opt => opt.UseNpgsql(fixture.ConnectionString));
        services.AddScoped<IHomeChefProDbContext>(sp => sp.GetRequiredService<HomeChefProDbContext>());
        services.AddSingleton<ICurrentUser>(new FakeCurrentUser(currentUserId ?? Guid.NewGuid(), "Admin"));
        // F-23: stub de IUploadUrlBuilder para tests via MediatR direct (no llaman AddInfrastructure).
        services.AddSingleton<HomeChefPro.Application.Uploads.Abstractions.IUploadUrlBuilder>(
            new TestUploadUrlBuilder());
        _root = services.BuildServiceProvider();
        Scope = _root.CreateScope();
    }

    public IMediator Mediator => Scope.ServiceProvider.GetRequiredService<IMediator>();
    public HomeChefProDbContext Db => Scope.ServiceProvider.GetRequiredService<HomeChefProDbContext>();

    public async ValueTask DisposeAsync()
    {
        Scope.Dispose();
        await _root.DisposeAsync();
    }

    private sealed class TestUploadUrlBuilder : HomeChefPro.Application.Uploads.Abstractions.IUploadUrlBuilder
    {
        public string BuildPaymentProofUrl(Guid chefId, string filename) =>
            $"/api/uploads/{chefId:N}/payment-proofs/{filename}";
        public string BuildRecipeImageUrl(Guid chefId, string filename) =>
            $"/api/uploads/{chefId:N}/recipes/{filename}";
    }

        private sealed class NullLoggerProvider : ILoggerProvider
    {
        public static NullLoggerProvider Instance { get; } = new();
        public ILogger CreateLogger(string categoryName) => Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        public void Dispose() { }
    }
}
