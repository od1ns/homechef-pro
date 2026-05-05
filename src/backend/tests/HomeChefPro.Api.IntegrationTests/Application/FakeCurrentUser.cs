using HomeChefPro.Application.Abstractions;

namespace HomeChefPro.Api.IntegrationTests.Application;

public sealed class FakeCurrentUser(Guid userId, params string[] roles) : ICurrentUser
{
    public Guid? UserId => userId;
    public string? Email { get; init; }
    public bool IsAuthenticated => userId != Guid.Empty;
    public IReadOnlyCollection<string> Roles { get; } = roles;
    public bool IsInRole(string role) => Roles.Contains(role);

    /// <summary>
    /// Pasada C / Fase 1C-A: en tests single-tenant, defaultear al chef piloto.
    /// Override via init si algun test necesita simular un chef distinto.
    /// </summary>
    public Guid? ChefId { get; init; } = HomeChefPro.Domain.Tenancy.Chef.PilotoId;

    public Guid RequireUserId() =>
        userId == Guid.Empty
            ? throw new UnauthorizedAccessException()
            : userId;

    public Guid RequireChefId() =>
        ChefId ?? throw new UnauthorizedAccessException("FakeCurrentUser has no ChefId.");
}
