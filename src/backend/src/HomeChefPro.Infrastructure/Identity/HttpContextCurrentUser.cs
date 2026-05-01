using System.Security.Claims;
using HomeChefPro.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace HomeChefPro.Infrastructure.Identity;

public sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var principal = Principal;
            if (principal is null) return null;
            var value = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? principal.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray() ?? [];

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public Guid RequireUserId() =>
        UserId ?? throw new UnauthorizedAccessException("No authenticated user on the current request.");
}
