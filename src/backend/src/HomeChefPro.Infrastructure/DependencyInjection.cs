using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Invoicing.Abstractions;
using HomeChefPro.Application.Invoicing.Commands.EmitInvoice;
using HomeChefPro.Application.Receipts.Abstractions;
using HomeChefPro.Application.Uploads.Abstractions;
using HomeChefPro.Infrastructure.Auth;
using HomeChefPro.Infrastructure.Identity;
using HomeChefPro.Infrastructure.Invoicing;
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
        })
        .AddRoles<IdentityRole<Guid>>()
        .AddRoleManager<RoleManager<IdentityRole<Guid>>>()
        .AddEntityFrameworkStores<HomeChefProDbContext>();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IReceiptPdfGenerator, QuestPdfReceiptGenerator>();

        services.Configure<LocalFileStorageOptions>(
            configuration.GetSection(LocalFileStorageOptions.SectionName));
        services.AddSingleton<IFileStorage, LocalFileStorage>();

        services.Configure<TaxOptions>(configuration.GetSection(TaxOptions.SectionName));
        services.Configure<IssuerOptions>(configuration.GetSection(IssuerOptions.SectionName));
        services.AddSingleton<IFiscalProvider, MockFiscalProvider>();
        services.AddSingleton(sp =>
        {
            var tax = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TaxOptions>>().Value;
            var issuer = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<IssuerOptions>>().Value;
            return new InvoicingSettings(
                IvaRate: tax.IvaRate,
                IgtfRate: tax.IgtfRate,
                IgtfPaymentMethods: tax.IgtfPaymentMethods,
                IssuerRif: issuer.Rif,
                IssuerLegalName: issuer.LegalName,
                IssuerAddress: issuer.Address);
        });

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
