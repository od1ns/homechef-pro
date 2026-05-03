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

public sealed record CustomerRankingRow(
    string CustomerKey,        // user_id o guest_customer_id como string
    string CustomerType,       // "registered" | "guest"
    string DisplayName,
    string? Email,
    string? Phone,
    int OrdersCount,
    decimal LifetimeSpendUsd,
    decimal AvgTicketUsd,
    DateTimeOffset FirstOrderAt,
    DateTimeOffset LastOrderAt,
    int DaysSinceLastOrder,
    int OrdersLast90d,
    decimal SpendLast90d,
    string Segment);            // "vip" | "regular" | "casual" | "dormido"

public sealed record PeakHourCellRow(
    int DayOfWeek,    // 0=domingo, 6=sabado (Postgres DOW)
    int HourOfDay,    // 0-23 en zona Caracas
    int OrdersCount,
    decimal RevenueUsd,
    decimal AvgTicketUsd);

public sealed record PeakHourSummaryRow(
    int DayOfWeek,
    int PeakHour,
    int PeakOrdersCount,
    decimal PeakRevenueUsd);

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
