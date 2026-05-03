using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Reports.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Reports.Queries;

// ---- Dish profit margin ----
public sealed record DishProfitMarginQuery : IRequest<IReadOnlyList<DishProfitMarginRow>>;
public sealed class DishProfitMarginHandler(IHomeChefProDbContext db)
    : IRequestHandler<DishProfitMarginQuery, IReadOnlyList<DishProfitMarginRow>>
{
    public async Task<IReadOnlyList<DishProfitMarginRow>> Handle(
        DishProfitMarginQuery request, CancellationToken ct)
    {
        // CAST a numeric(N,M) en TODAS las columnas decimales: la vista calcula
        // ratios como (price/cost) que pueden tener >29 digitos significativos
        // y System.Decimal solo aguanta 28-29 -> OverflowException sin el cast.
        var rows = await ((DbContext)db).Database
            .SqlQueryRaw<DishProfitMarginRow>(@"
                SELECT
                    dish_id                                       AS ""DishId"",
                    name                                          AS ""Name"",
                    selling_price_usd::numeric(14,4)              AS ""SellingPriceUsd"",
                    COALESCE(total_cost_usd, 0)::numeric(14,4)    AS ""TotalCostUsd"",
                    COALESCE(gross_profit_usd, 0)::numeric(14,4)  AS ""GrossProfitUsd"",
                    COALESCE(gross_margin_pct, 0)::numeric(14,4)  AS ""GrossMarginPct"",
                    price_to_cost_ratio::numeric(14,4)            AS ""PriceToCostRatio""
                FROM dish_profit_margin
                ORDER BY gross_margin_pct DESC NULLS LAST")
            .ToListAsync(ct).ConfigureAwait(false);
        return rows;
    }
}

// ---- Reorder suggestions ----
public sealed record ReorderSuggestionsQuery(string? PriorityFilter = null)
    : IRequest<IReadOnlyList<ReorderSuggestionRow>>;
public sealed class ReorderSuggestionsHandler(IHomeChefProDbContext db)
    : IRequestHandler<ReorderSuggestionsQuery, IReadOnlyList<ReorderSuggestionRow>>
{
    private static readonly string[] AllowedPriorities = ["critical", "urgent", "soon", "ok"];

    public async Task<IReadOnlyList<ReorderSuggestionRow>> Handle(
        ReorderSuggestionsQuery request, CancellationToken ct)
    {
        var ctx = (DbContext)db;
        var rows = await ctx.Database.SqlQueryRaw<ReorderSuggestionRow>(@"
                SELECT
                    ingredient_id                                                  AS ""IngredientId"",
                    name                                                           AS ""Name"",
                    use_unit                                                       AS ""UseUnit"",
                    current_stock_use_unit::numeric(14,4)                          AS ""CurrentStockUseUnit"",
                    reorder_point_use_unit::numeric(14,4)                          AS ""ReorderPointUseUnit"",
                    minimum_stock_use_unit::numeric(14,4)                          AS ""MinimumStockUseUnit"",
                    avg_cost_per_use_unit_usd::numeric(14,6)                       AS ""AvgCostPerUseUnitUsd"",
                    COALESCE(avg_daily_consumption, 0)::numeric(14,4)              AS ""AvgDailyConsumption"",
                    estimated_days_until_stockout::numeric(14,2)                   AS ""EstimatedDaysUntilStockout"",
                    priority                                                       AS ""Priority""
                FROM ingredient_reorder_suggestions
                ORDER BY
                    CASE priority
                        WHEN 'critical' THEN 0
                        WHEN 'urgent'   THEN 1
                        WHEN 'soon'     THEN 2
                        ELSE 3
                    END,
                    avg_daily_consumption DESC")
            .ToListAsync(ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.PriorityFilter)
            && AllowedPriorities.Contains(request.PriorityFilter, StringComparer.OrdinalIgnoreCase))
        {
            rows = rows.Where(r =>
                string.Equals(r.Priority, request.PriorityFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return rows;
    }
}

// ---- Sales daily summary ----
public sealed record SalesDailyQuery(int Days = 30) : IRequest<IReadOnlyList<SalesDailyRow>>;
public sealed class SalesDailyHandler(IHomeChefProDbContext db)
    : IRequestHandler<SalesDailyQuery, IReadOnlyList<SalesDailyRow>>
{
    public async Task<IReadOnlyList<SalesDailyRow>> Handle(SalesDailyQuery request, CancellationToken ct)
    {
        var days = Math.Clamp(request.Days, 1, 90);
        var rows = await ((DbContext)db).Database.SqlQueryRaw<SalesDailyRow>(@"
                SELECT
                    sale_date         AS ""SaleDate"",
                    orders_count      AS ""OrdersCount"",
                    COALESCE(revenue_usd, 0)::numeric(14,4)      AS ""RevenueUsd"",
                    COALESCE(gross_profit_usd, 0)::numeric(14,4) AS ""GrossProfitUsd""
                FROM sales_daily_summary
                WHERE sale_date >= CURRENT_DATE - INTERVAL '1 day' * {0}
                ORDER BY sale_date DESC", days)
            .ToListAsync(ct).ConfigureAwait(false);
        return rows;
    }
}

// ---- Recipe full cost ----
public sealed record RecipeFullCostsQuery(bool IncludeSubRecipes = false)
    : IRequest<IReadOnlyList<RecipeFullCostRow>>;
public sealed class RecipeFullCostsHandler(IHomeChefProDbContext db)
    : IRequestHandler<RecipeFullCostsQuery, IReadOnlyList<RecipeFullCostRow>>
{
    public async Task<IReadOnlyList<RecipeFullCostRow>> Handle(
        RecipeFullCostsQuery request, CancellationToken ct)
    {
        var sql = @"
            SELECT
                recipe_id                                  AS ""RecipeId"",
                name                                       AS ""Name"",
                is_sub_recipe                              AS ""IsSubRecipe"",
                COALESCE(total_cost_usd, 0)::numeric(14,4) AS ""TotalCostUsd""
            FROM recipe_full_cost_usd
            " + (request.IncludeSubRecipes ? "" : "WHERE is_sub_recipe = FALSE ")
              + "ORDER BY total_cost_usd DESC";

        var rows = await ((DbContext)db).Database.SqlQueryRaw<RecipeFullCostRow>(sql)
            .ToListAsync(ct).ConfigureAwait(false);
        return rows;
    }
}

// ---- Inventory rotation ----
public sealed record InventoryRotationQuery(string? CategoryFilter = null)
    : IRequest<IReadOnlyList<InventoryRotationRow>>;

public sealed class InventoryRotationHandler(IHomeChefProDbContext db)
    : IRequestHandler<InventoryRotationQuery, IReadOnlyList<InventoryRotationRow>>
{
    private static readonly string[] AllowedCategories = ["alta", "media", "baja", "inactivo"];

    public async Task<IReadOnlyList<InventoryRotationRow>> Handle(
        InventoryRotationQuery request, CancellationToken ct)
    {
        var rows = await ((DbContext)db).Database.SqlQueryRaw<InventoryRotationRow>(@"
                SELECT
                    ingredient_id                                   AS ""IngredientId"",
                    name                                            AS ""Name"",
                    use_unit                                        AS ""UseUnit"",
                    current_stock_use_unit::numeric(14,4)           AS ""CurrentStockUseUnit"",
                    avg_cost_per_use_unit_usd::numeric(14,6)        AS ""AvgCostPerUseUnitUsd"",
                    COALESCE(stock_value_usd, 0)::numeric(14,4)     AS ""StockValueUsd"",
                    COALESCE(consumed_last_90d, 0)::numeric(14,4)   AS ""ConsumedLast90d"",
                    COALESCE(daily_avg_consumption, 0)::numeric(14,4) AS ""DailyAvgConsumption"",
                    days_of_stock::numeric(14,2)                    AS ""DaysOfStock"",
                    annual_turnover::numeric(14,2)                  AS ""AnnualTurnover"",
                    last_purchased_at                               AS ""LastPurchasedAt"",
                    last_consumed_at                                AS ""LastConsumedAt"",
                    rotation_category                               AS ""RotationCategory""
                FROM ingredient_rotation_report
                ORDER BY
                    CASE rotation_category
                        WHEN 'inactivo' THEN 0  -- mostrar primero los problematicos
                        WHEN 'baja'     THEN 1
                        WHEN 'media'    THEN 2
                        WHEN 'alta'     THEN 3
                        ELSE 4
                    END,
                    stock_value_usd DESC NULLS LAST")
            .ToListAsync(ct).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(request.CategoryFilter)
            && AllowedCategories.Contains(request.CategoryFilter, StringComparer.OrdinalIgnoreCase))
        {
            rows = rows.Where(r =>
                string.Equals(r.RotationCategory, request.CategoryFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return rows;
    }
}

// ---- Kitchen queue ----
public sealed record KitchenQueueQuery : IRequest<IReadOnlyList<KitchenQueueRow>>;
public sealed class KitchenQueueHandler(IHomeChefProDbContext db)
    : IRequestHandler<KitchenQueueQuery, IReadOnlyList<KitchenQueueRow>>
{
    public async Task<IReadOnlyList<KitchenQueueRow>> Handle(KitchenQueueQuery request, CancellationToken ct)
    {
        var rows = await ((DbContext)db).Database.SqlQueryRaw<KitchenQueueRow>(@"
                SELECT
                    order_id                AS ""OrderId"",
                    order_number            AS ""OrderNumber"",
                    order_status            AS ""OrderStatus"",
                    scheduled_for           AS ""ScheduledFor"",
                    prep_estimated_ready_at AS ""PrepEstimatedReadyAt"",
                    customer_notes          AS ""CustomerNotes"",
                    order_item_id           AS ""OrderItemId"",
                    dish_id                 AS ""DishId"",
                    dish_name_snapshot      AS ""DishNameSnapshot"",
                    quantity                AS ""Quantity"",
                    item_notes              AS ""ItemNotes"",
                    kitchen_status          AS ""KitchenStatus"",
                    prep_started_at         AS ""PrepStartedAt"",
                    procedure_markdown      AS ""ProcedureMarkdown"",
                    prep_time_minutes       AS ""PrepTimeMinutes"",
                    priority_time           AS ""PriorityTime""
                FROM kitchen_active_queue")
            .ToListAsync(ct).ConfigureAwait(false);
        return rows;
    }
}
