namespace HomeChefPro.Application.Loyalty.Dtos;

public sealed record LoyaltyAccountDto(
    int CurrentBalance,
    int LifetimeEarned,
    string Level,           // bronce | plata | oro
    int PointsToNextLevel,  // 0 si ya esta en oro
    string? NextLevel);     // null si ya esta en oro

public sealed record LoyaltyRewardDto(
    Guid Id,
    string Name,
    string? Description,
    int CostPoints,
    string RewardType,
    string? RewardValue,
    bool IsAffordable);     // true si el balance actual del usuario alcanza

public sealed record LoyaltyTransactionDto(
    Guid Id,
    string Type,            // earn | redeem | adjust
    int Points,
    Guid? RelatedOrderId,
    Guid? RelatedRewardId,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record RedeemRewardResultDto(
    Guid TransactionId,
    Guid RewardId,
    string RewardName,
    int PointsSpent,
    int RemainingBalance);
