using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Domain.Identity;
using HomeChefPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Api.Auth;

/// <summary>
/// Seeds the four built-in roles and an initial admin user on application startup.
/// Admin credentials come from <c>Bootstrap:Admin:Email</c> and <c>Bootstrap:Admin:Password</c>.
/// If either is missing, the admin is not created — explicit opt-in only.
/// </summary>
public static class AuthBootstrap
{
    public static async Task EnsureRolesAndAdminAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var identity = sp.GetRequiredService<IIdentityService>();
        await identity.EnsureRolesExistAsync([
            Roles.Admin, Roles.Cashier, Roles.Cook, Roles.Client,
        ]).ConfigureAwait(false);

        var config = sp.GetRequiredService<IConfiguration>();
        var adminEmail = config["Bootstrap:Admin:Email"];
        var adminPassword = config["Bootstrap:Admin:Password"];
        var adminName = config["Bootstrap:Admin:FullName"] ?? "Admin";

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            return;

        var db = sp.GetRequiredService<HomeChefProDbContext>();
        var exists = await db.Users.AnyAsync(u => u.Email == adminEmail).ConfigureAwait(false);
        if (exists) return;

        var userId = Guid.NewGuid();
        var op = await identity.CreateUserAsync(
            userId: userId,
            email: adminEmail,
            password: adminPassword,
            phone: null,
            roles: [Roles.Admin]).ConfigureAwait(false);

        if (!op.Succeeded)
            throw new InvalidOperationException(
                $"Failed to bootstrap admin: {string.Join("; ", op.Errors)}");

        db.UserProfiles.Add(UserProfile.Create(
            userId: userId,
            fullName: adminName,
            preferredLanguage: "es-VE"));
        await db.SaveChangesAsync().ConfigureAwait(false);
    }
}
