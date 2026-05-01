using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Auth.Dtos;
using HomeChefPro.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Auth.Queries.GetMe;

public sealed record GetMeQuery : IRequest<UserSummaryDto>;

public sealed class GetMeHandler(
    IHomeChefProDbContext db,
    IIdentityService identity,
    ICurrentUser currentUser)
    : IRequestHandler<GetMeQuery, UserSummaryDto>
{
    public async Task<UserSummaryDto> Handle(GetMeQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == userId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(HomeChefPro.Domain.Identity.UserProfile), userId);

        var roles = await identity.GetRolesAsync(userId, ct).ConfigureAwait(false);

        return new UserSummaryDto(
            UserId: profile.Id,
            Email: currentUser.Email ?? string.Empty,
            FullName: profile.FullName,
            DefaultPhone: profile.DefaultPhone,
            PreferredLanguage: profile.PreferredLanguage,
            Roles: [.. roles]);
    }
}
