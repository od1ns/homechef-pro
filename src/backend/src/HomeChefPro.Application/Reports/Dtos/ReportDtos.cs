namespace HomeChefPro.Application.Reports.Dtos;

public sealed record DishProfitMarginRow(
    Guid DishId,
    string Name,
    decimal? SellingPriceUsd,
    decimal TotalCostUsd,
    decimal GrossProfitUsd,
    decimal GrossMarginPct,
    decimal? PriceToCostRatio);

public sealed record ReorderSuggestionRow(
    Guid IngredientId,
    string Name,
    string UseUnit,
    decimal CurrentStockUseUnit,
    decimal ReorderPointUseUnit,
    decimal MinimumStockUseUnit,
    decimal AvgCostPerUseUnitUsd,
    decimal AvgDailyConsumption,
    decimal? EstimatedDaysUntilStockout,
    string Priority);

public sealed record SalesDailyRow(
    DateOnly SaleDate,
    int OrdersCount,
    decimal RevenueUsd,
    decimal GrossProfitUsd);

public sealed record RecipeFullCostRow(
    Guid RecipeId,
    string Name,
    bool IsSubRecipe,
    decimal TotalCostUsd);

public sealed record InventoryRotationRow(
    Guid IngredientId,
    string Name,
    string UseUnit,
    decimal CurrentStockUseUnit,
    decimal AvgCostPerUseUnitUsd,
    decimal StockValueUsd,
    decimal ConsumedLast90d,
    decimal DailyAvgConsumption,
    decimal? DaysOfStock,
    decimal? AnnualTurnover,
    DateOnly? LastPurchasedAt,
    DateOnly? LastConsumedAt,
    string RotationCategory);

public sealed record KitchenQueueRow(
    Guid OrderId,
    string OrderNumber,
    string OrderStatus,
    DateTimeOffset? ScheduledFor,
    DateTimeOffset? PrepEstimatedReadyAt,
    string? CustomerNotes,
    Guid OrderItemId,
    Guid DishId,
    string DishNameSnapshot,
    int Quantity,
    string? ItemNotes,
    string KitchenStatus,
    DateTimeOffset? PrepStartedAt,
    string? ProcedureMarkdown,
    int PrepTimeMinutes,
    DateTimeOffset PriorityTime);
