using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace HomeChefPro.Infrastructure.Auth;

public sealed class IdentityService(
    UserManager<AppUser> users,
    RoleManager<IdentityRole<Guid>> roles) : IIdentityService
{
    private readonly UserManager<AppUser> _users = users;
    private readonly RoleManager<IdentityRole<Guid>> _roles = roles;

    public async Task<IdentityOperation> CreateUserAsync(
        Guid userId,
        string email,
        string password,
        string? phone,
        IReadOnlyCollection<string> roles,
        CancellationToken ct = default)
    {
        var existing = await _users.FindByEmailAsync(email).ConfigureAwait(false);
        if (existing is not null)
            return IdentityOperation.Fail($"Email '{email}' is already registered.");

        var user = new AppUser
        {
            Id = userId,
            UserName = email,
            Email = email,
            PhoneNumber = phone,
            EmailConfirmed = true,          // admin-created; no email flow yet
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
        };

        var result = await _users.CreateAsync(user, password).ConfigureAwait(false);
        if (!result.Succeeded)
            return IdentityOperation.Fail(result.Errors.Select(e => e.Description).ToArray());

        if (roles.Count > 0)
        {
            await EnsureRolesExistAsync(roles, ct).ConfigureAwait(false);
            var addRoles = await _users.AddToRolesAsync(user, roles).ConfigureAwait(false);
            if (!addRoles.Succeeded)
                return IdentityOperation.Fail(addRoles.Errors.Select(e => e.Description).ToArray());
        }
        return IdentityOperation.Ok;
    }

    public async Task<IdentityOperation> SetPasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null)
            return IdentityOperation.Fail("User not found.");

        var result = await _users.ChangePasswordAsync(user, currentPassword, newPassword).ConfigureAwait(false);
        return result.Succeeded
            ? IdentityOperation.Ok
            : IdentityOperation.Fail(result.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<SignInAttempt> VerifyPasswordAsync(
        string email,
        string password,
        CancellationToken ct = default)
    {
        var user = await _users.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null)
            return new SignInAttempt(false, null, null, [], "Invalid credentials.");

        var valid = await _users.CheckPasswordAsync(user, password).ConfigureAwait(false);
        if (!valid)
            return new SignInAttempt(false, null, null, [], "Invalid credentials.");

        if (!user.EmailConfirmed)
            return new SignInAttempt(false, null, null, [], "Email not confirmed.");

        var userRoles = await _users.GetRolesAsync(user).ConfigureAwait(false);
        return new SignInAttempt(true, user.Id, user.Email, [.. userRoles], null);
    }

    public async Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return [];
        var list = await _users.GetRolesAsync(user).ConfigureAwait(false);
        return [.. list];
    }

    public async Task<IdentityOperation> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return IdentityOperation.Fail("User not found.");
        await EnsureRolesExistAsync([role], ct).ConfigureAwait(false);
        var r = await _users.AddToRoleAsync(user, role).ConfigureAwait(false);
        return r.Succeeded ? IdentityOperation.Ok : IdentityOperation.Fail(r.Errors.Select(e => e.Description).ToArray());
    }

    public async Task<IdentityOperation> RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return IdentityOperation.Fail("User not found.");
        var r = await _users.RemoveFromRoleAsync(user, role).ConfigureAwait(false);
        return r.Succeeded ? IdentityOperation.Ok : IdentityOperation.Fail(r.Errors.Select(e => e.Description).ToArray());
    }

    public async Task EnsureRolesExistAsync(IReadOnlyCollection<string> roles, CancellationToken ct = default)
    {
        foreach (var role in roles.Distinct(StringComparer.Ordinal))
        {
            if (!await _roles.RoleExistsAsync(role).ConfigureAwait(false))
            {
                await _roles.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() })
                    .ConfigureAwait(false);
            }
        }
    }
}
