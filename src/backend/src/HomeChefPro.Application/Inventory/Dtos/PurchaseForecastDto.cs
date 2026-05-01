namespace HomeChefPro.Application.Inventory.Dtos;

public sealed record PurchaseForecastDto(
    DateTimeOffset HistoricalFrom,
    DateTimeOffset HistoricalTo,
    int HistoricalDays,
    int TargetDays,
    decimal GrowthFactor,
    int OrdersAnalyzed,
    IReadOnlyList<PurchaseForecastLineDto> Lines);

public sealed record PurchaseForecastLineDto(
    Guid IngredientId,
    string IngredientName,
    string UseUnit,
    decimal HistoricalConsumedUseUnit,   // total consumed in historical window
    decimal DailyAverageUseUnit,         // convenience
    decimal ProjectedUseUnit,            // for target window, already scaled & growth applied
    decimal CurrentStockUseUnit,
    decimal ReorderPointUseUnit,
    decimal SuggestedPurchaseUseUnit,    // max(0, projected - currentStock), bumped to cover reorder_point
    decimal? LastPurchasePriceUsd,       // from the most-used presentation (hint to the admin)
    decimal? AvgCostPerUseUnitUsd,
    decimal? EstimatedCostUsd);          // suggestedPurchaseUseUnit * avgCost, if known
