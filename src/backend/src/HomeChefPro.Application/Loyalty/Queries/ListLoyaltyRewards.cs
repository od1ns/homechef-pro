using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Loyalty.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Loyalty.Queries;

/// <summary>
/// Lista las recompensas activas con un flag isAffordable que dice si el
/// balance actual del usuario alcanza para canjearla.
/// </summary>
public sealed record ListLoyaltyRewardsQuery : IRequest<IReadOnlyList<LoyaltyRewardDto>>;

public sealed class ListLoyaltyRewardsHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<ListLoyaltyRewardsQuery, IReadOnlyList<LoyaltyRewardDto>>
{
    public async Task<IReadOnlyList<LoyaltyRewardDto>> Handle(
        ListLoyaltyRewardsQuery request, CancellationToken ct)
    {
        var ctx = (DbContext)db;

        // Saldo del usuario (puede no tener cuenta si nunca compró).
        var userId = currentUser.UserId;
        var balance = 0;
        if (userId is { } uid)
        {
            var balanceRows = await ctx.Database.SqlQueryRaw<int>(
                @"SELECT current_balance FROM loyalty_accounts WHERE user_id = {0}", uid)
                .ToListAsync(ct).ConfigureAwait(false);
            balance = balanceRows.Count > 0 ? balanceRows[0] : 0;
        }

        var rows = await ctx.Database.SqlQueryRaw<RawReward>(@"
            SELECT
                id           AS ""Id"",
                name         AS ""Name"",
                description  AS ""Description"",
                cost_points  AS ""CostPoints"",
                reward_type  AS ""RewardType"",
                reward_value AS ""RewardValue""
            FROM loyalty_rewards
            WHERE is_active = TRUE
            ORDER BY cost_points ASC")
            .ToListAsync(ct).ConfigureAwait(false);

        return rows
            .Select(r => new LoyaltyRewardDto(
                r.Id, r.Name, r.Description, r.CostPoints, r.RewardType, r.RewardValue,
                IsAffordable: balance >= r.CostPoints))
            .ToList();
    }

    private sealed record RawReward(
        Guid Id, string Name, string? Description, int CostPoints,
        string RewardType, string? RewardValue);
}
