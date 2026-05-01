namespace HomeChefPro.Application.Auth.Abstractions;

public interface IIdentityService
{
    /// <summary>
    /// Creates an auth identity (AspNetUsers row). Does NOT touch the UserProfile row;
    /// the caller is responsible for creating it alongside.
    /// </summary>
    Task<IdentityOperation> CreateUserAsync(
        Guid userId,
        string email,
        string password,
        string? phone,
        IReadOnlyCollection<string> roles,
        CancellationToken ct = default);

    Task<IdentityOperation> SetPasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default);

    Task<SignInAttempt> VerifyPasswordAsync(
        string email,
        string password,
        CancellationToken ct = default);

    Task<IReadOnlyCollection<string>> GetRolesAsync(Guid userId, CancellationToken ct = default);

    Task<IdentityOperation> AssignRoleAsync(Guid userId, string role, CancellationToken ct = default);
    Task<IdentityOperation> RemoveRoleAsync(Guid userId, string role, CancellationToken ct = default);

    Task EnsureRolesExistAsync(IReadOnlyCollection<string> roles, CancellationToken ct = default);
}

public sealed record IdentityOperation(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static IdentityOperation Ok { get; } = new(true, []);
    public static IdentityOperation Fail(params string[] errors) => new(false, errors);
}

public sealed record SignInAttempt(
    bool Succeeded,
    Guid? UserId,
    string? Email,
    IReadOnlyCollection<string> Roles,
    string? FailureReason);
