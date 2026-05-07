using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Invitations.Commands.CreateInvitation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Invitations.Queries.ListInvitations;

/// <summary>
/// Lista codigos de invitacion. Si onlyActive, filtra por activos
/// (no revocados, no expirados, no exhaustos). Si chefId, solo de ese chef.
/// </summary>
public sealed record ListInvitationsQuery(
    bool OnlyActive = true,
    Guid? ChefId = null,
    int PageSize = 50) : IRequest<IReadOnlyList<InvitationCodeDto>>;

public sealed class ListInvitationsHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<ListInvitationsQuery, IReadOnlyList<InvitationCodeDto>>
{
    public async Task<IReadOnlyList<InvitationCodeDto>> Handle(ListInvitationsQuery request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var q = db.InvitationCodes.AsNoTracking().AsQueryable();

        if (request.ChefId is { } chefId)
            q = q.Where(i => i.ChefId == chefId);

        if (request.OnlyActive)
        {
            q = q.Where(i =>
                i.RevokedAt == null
                && (i.ExpiresAt == null || i.ExpiresAt > now)
                && i.UsedCount < i.MaxUses);
        }

        var list = await q.OrderByDescending(i => i.CreatedAt)
            .Take(Math.Clamp(request.PageSize, 1, 500))
            .ToListAsync(ct).ConfigureAwait(false);

        return [.. list.Select(i => CreateInvitationHandler.ToDto(i, now))];
    }
}
