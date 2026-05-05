namespace HomeChefPro.Application.Abstractions;

/// <summary>
/// Information about the caller making the current request. Infrastructure backs this with
/// <c>IHttpContextAccessor</c>; tests can stub it directly.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Pasada C / Fase 1C-A: tenant del usuario, leido del claim "chef_id" del JWT.
    /// Null si el caller no esta autenticado o el JWT carece del claim (legacy).
    /// </summary>
    Guid? ChefId { get; }

    /// <summary>
    /// Returns the UserId or throws if there is no authenticated user — use when the caller
    /// is known to be authenticated (e.g. inside an [Authorize] endpoint handler).
    /// </summary>
    Guid RequireUserId();

    /// <summary>
    /// Returns the ChefId or throws — use cuando se sabe que el caller es staff
    /// (Admin/Cashier/Cook) y debe operar contra SU chef. En Fase 2 (cuando entren
    /// multiples chefs) esto bloquea operaciones cross-tenant.
    /// </summary>
    Guid RequireChefId();
}
