using Microsoft.AspNetCore.Identity;

namespace HomeChefPro.Infrastructure.Identity;

/// <summary>
/// ASP.NET Core Identity user for HomeChef Pro. Extended profile fields
/// live in <c>UserProfile</c> (1:1 by Id).
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
}
