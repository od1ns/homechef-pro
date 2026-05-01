using HomeChefPro.Application.Abstractions;

namespace HomeChefPro.Api.IntegrationTests.Application;

public sealed class FakeCurrentUser(Guid userId, params string[] roles) : ICurrentUser
{
    public Guid? UserId => userId;
    public string? Email { get; init; }
    public bool IsAuthenticated => userId != Guid.Empty;
    public IReadOnlyCollection<string> Roles { get; } = roles;
    public bool IsInRole(string role) => Roles.Contains(role);
    public Guid RequireUserId() =>
        userId == Guid.Empty
            ? throw new UnauthorizedAccessException()
            : userId;
}
