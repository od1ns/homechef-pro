using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Loyalty.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Loyalty.Queries;

/// <summary>
/// Devuelve el saldo, nivel, y puntos faltantes para el siguiente nivel del
/// usuario actualmente autenticado. Si el usuario nunca acumulo puntos,
/// retorna 0/bronce con metas a alcanzar.
/// </summary>
public sealed record GetLoyaltyAccountQuery : IRequest<LoyaltyAccountDto>;

public sealed class GetLoyaltyAccountHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<GetLoyaltyAccountQuery, LoyaltyAccountDto>
{
    public async Task<LoyaltyAccountDto> Handle(GetLoyaltyAccountQuery request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();

        var ctx = (DbContext)db;
        var rows = await ctx.Database.SqlQueryRaw<RawAccount>(
            @"SELECT current_balance AS ""CurrentBalance"",
                     lifetime_earned AS ""LifetimeEarned"",
                     level           AS ""Level""
              FROM loyalty_accounts
              WHERE user_id = {0}",
            userId)
            .ToListAsync(ct).ConfigureAwait(false);

        var (balance, lifetime, level) = rows.Count == 0
            ? (0, 0, "bronce")
            : (rows[0].CurrentBalance, rows[0].LifetimeEarned, rows[0].Level);

        return ComputeDto(balance, lifetime, level);
    }

    private static LoyaltyAccountDto ComputeDto(int balance, int lifetime, string level)
    {
        return level switch
        {
            "oro"    => new(balance, lifetime, level, 0, null),
            "plata"  => new(balance, lifetime, level, Math.Max(0, 1000 - lifetime), "oro"),
            _       /* bronce */ => new(balance, lifetime, level, Math.Max(0, 500 - lifetime), "plata"),
        };
    }

    private sealed record RawAccount(int CurrentBalance, int LifetimeEarned, string Level);
}
