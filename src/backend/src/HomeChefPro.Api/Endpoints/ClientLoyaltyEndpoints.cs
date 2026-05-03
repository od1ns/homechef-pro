using HomeChefPro.Application.Loyalty.Commands;
using HomeChefPro.Application.Loyalty.Dtos;
using HomeChefPro.Application.Loyalty.Queries;
using MediatR;

namespace HomeChefPro.Api.Endpoints;

/// <summary>
/// Endpoints del programa de fidelidad "Sabor". Todos requieren autenticacion
/// (los clientes guest no acumulan puntos — no hay forma de identificarlos).
/// </summary>
public static class ClientLoyaltyEndpoints
{
    public static IEndpointRouteBuilder MapClientLoyaltyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/client/loyalty")
            .WithTags("Client: Loyalty (Sabor)")
            .RequireAuthorization();

        // Saldo + nivel + puntos al siguiente nivel
        group.MapGet("me", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetLoyaltyAccountQuery(), ct)))
            .WithName("GetLoyaltyAccount")
            .Produces<LoyaltyAccountDto>();

        // Catalogo de recompensas activas
        group.MapGet("rewards", async (IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new ListLoyaltyRewardsQuery(), ct)))
            .WithName("ListLoyaltyRewards")
            .Produces<IReadOnlyList<LoyaltyRewardDto>>();

        // Canjear una recompensa
        group.MapPost("redeem/{rewardId:guid}", async (
            Guid rewardId,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(new RedeemRewardCommand(rewardId), ct);
            return Results.Ok(result);
        })
        .WithName("RedeemReward")
        .Produces<RedeemRewardResultDto>();

        return app;
    }
}
