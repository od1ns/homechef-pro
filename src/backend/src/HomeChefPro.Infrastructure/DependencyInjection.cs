using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Invoicing.Abstractions;
using HomeChefPro.Application.Invoicing.Commands.EmitInvoice;
using HomeChefPro.Application.Receipts.Abstractions;
using HomeChefPro.Application.Uploads.Abstractions;
using HomeChefPro.Infrastructure.Auth;
using HomeChefPro.Infrastructure.Identity;
using HomeChefPro.Infrastructure.Invoicing;
using HomeChefPro.Infrastructure.Notifications;
using HomeChefPro.Infrastructure.Persistence;
using HomeChefPro.Infrastructure.Receipts;
using HomeChefPro.Infrastructure.Uploads;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HomeChefPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnection = configuration.GetConnectionString("PostgreSQL")
            ?? throw new InvalidOperationException("Missing connection string 'PostgreSQL'.");

        services.AddDbContext<HomeChefProDbContext>(options =>
        {
            options.UseNpgsql(postgresConnection, npg =>
            {
                npg.MigrationsHistoryTable("__ef_migrations_history", "public");
            });
        });

        services.AddSingleton<HomeChefPro.Application.Uploads.Abstractions.IUploadUrlBuilder, HomeChefPro.Infrastructure.Uploads.UploadUrlBuilder>();
        services.AddScoped<IHomeChefProDbContext>(sp => sp.GetRequiredService<HomeChefProDbContext>());
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddIdentityCore<AppUser>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.User.RequireUniqueEmail = true;
            // F-17: 2FA TOTP — token authenticator dura 30s window (default).
            options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
        .AddEntityFrameworkStores<HomeChefProDbContext>()
        .AddDefaultTokenProviders();  // F-17: registra AuthenticatorTokenProvider para TOTP

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ITotpService, TotpService>();  // F-17: MFA TOTP
        services.AddSingleton<IReceiptPdfGenerator, QuestPdfReceiptGenerator>();

        services.Configure<LocalFileStorageOptions>(
            configuration.GetSection(LocalFileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        // Pasada C / H-03: ya no hay IssuerOptions; el Issuer se lee del Chef
        // dentro del handler EmitInvoiceCommand.
        services.Configure<TaxOptions>(configuration.GetSection(TaxOptions.SectionName));
        services.AddSingleton<IFiscalProvider, MockFiscalProvider>();
        services.AddSingleton(sp =>
        {
            var tax = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TaxOptions>>().Value;
            return new InvoicingSettings(
                IvaRate: tax.IvaRate,
                IgtfRate: tax.IgtfRate,
                IgtfPaymentMethods: tax.IgtfPaymentMethods);
        });

        // Etapa 5: Firebase Cloud Messaging — opcional, se activa si existe el archivo de credenciales.
        var firebasePath = configuration["Firebase:ServiceAccountPath"];
        if (!string.IsNullOrWhiteSpace(firebasePath) && File.Exists(firebasePath))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile(firebasePath),
            });
            services.AddSingleton<INotificationService, FcmNotificationService>();
        }
        else
        {
            services.AddSingleton<INotificationService, NullNotificationService>();
        }

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "hcp:";
            });
        }

        return services;
    }
}
