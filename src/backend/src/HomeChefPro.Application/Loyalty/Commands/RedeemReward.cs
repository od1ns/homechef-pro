using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Loyalty.Dtos;
using HomeChefPro.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Loyalty.Commands;

/// <summary>
/// Canjea una recompensa: descuenta puntos del balance del usuario y registra
/// la transaction. Se hace en una transaccion para evitar dobles canjes.
/// </summary>
public sealed record RedeemRewardCommand(Guid RewardId) : IRequest<RedeemRewardResultDto>;

public sealed class RedeemRewardValidator : AbstractValidator<RedeemRewardCommand>
{
    public RedeemRewardValidator()
    {
        RuleFor(x => x.RewardId).NotEmpty();
    }
}

public sealed class RedeemRewardHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser)
    : IRequestHandler<RedeemRewardCommand, RedeemRewardResultDto>
{
    public async Task<RedeemRewardResultDto> Handle(RedeemRewardCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var ctx = (DbContext)db;

        // 1. Validar que la recompensa existe y esta activa.
        var reward = await ctx.Database.SqlQueryRaw<RewardRow>(
            @"SELECT id AS ""Id"", name AS ""Name"", cost_points AS ""CostPoints"", is_active AS ""IsActive""
              FROM loyalty_rewards WHERE id = {0}", request.RewardId)
            .ToListAsync(ct).ConfigureAwait(false);

        if (reward.Count == 0)
            throw new NotFoundException("LoyaltyReward", request.RewardId);
        var r = reward[0];
        if (!r.IsActive)
            throw new DomainException("Esta recompensa no esta disponible.");

        // 2. Atomicamente: descontar saldo si alcanza, registrar transaction.
        // El UPDATE devuelve 0 rows si el balance no alcanza, gracias al WHERE.
        var rowsAffected = await ctx.Database.ExecuteSqlRawAsync(
            @"UPDATE loyalty_accounts
              SET current_balance = current_balance - {1},
                  updated_at = now()
              WHERE user_id = {0} AND current_balance >= {1}",
            userId, r.CostPoints).ConfigureAwait(false);

        if (rowsAffected == 0)
            throw new DomainException("Saldo insuficiente para canjear esta recompensa.");

        // 3. Registrar la transaction y obtener el id.
        var txIdRows = await ctx.Database.SqlQueryRaw<Guid>(
            @"INSERT INTO loyalty_transactions
                (user_id, type, points, related_reward_id, notes)
              VALUES ({0}, 'redeem', {1}, {2}, {3})
              RETURNING id",
            userId, r.CostPoints, r.Id, $"Canje de '{r.Name}'")
            .ToListAsync(ct).ConfigureAwait(false);

        // 4. Releer el balance para devolverlo al cliente.
        var newBalance = await ctx.Database.SqlQueryRaw<int>(
            @"SELECT current_balance FROM loyalty_accounts WHERE user_id = {0}", userId)
            .ToListAsync(ct).ConfigureAwait(false);

        return new RedeemRewardResultDto(
            TransactionId: txIdRows[0],
            RewardId: r.Id,
            RewardName: r.Name,
            PointsSpent: r.CostPoints,
            RemainingBalance: newBalance.Count > 0 ? newBalance[0] : 0);
    }

    private sealed record RewardRow(Guid Id, string Name, int CostPoints, bool IsActive);
}
