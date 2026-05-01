namespace HomeChefPro.Application.Abstractions;

/// <summary>
/// Information about the caller making the current request. Infrastructure backs this with
/// <c>IHttpContextAccessor</c>; tests can stub it directly.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    IReadOnlyCollection<string> Roles { get; }

    /// <summary>
    /// Returns the UserId or throws if there is no authenticated user — use when the caller
    /// is known to be authenticated (e.g. inside an [Authorize] endpoint handler).
    /// </summary>
    Guid RequireUserId();
}
