using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Inventory.Dtos;
using HomeChefPro.Domain.Catalog.Ingredients;
using HomeChefPro.Domain.Catalog.Recipes.Services;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Inventory.Queries.ForecastPurchases;

/// <summary>
/// Predicts ingredient purchases for the next <paramref name="TargetDays"/> days based on
/// actually-delivered orders in the last <paramref name="HistoricalDays"/> days.
///
/// Formula per ingredient:
///   historical_total = Σ (order_item.quantity × flatten(recipe)[ingredient])
///   daily_avg        = historical_total / historical_days
///   projected        = daily_avg × target_days × growth_factor
///   suggested_buy    = max(0, projected − current_stock)
///                      bumped up if it would leave us below reorder_point
/// </summary>
public sealed record ForecastPurchasesQuery(
    int HistoricalDays = 28,
    int TargetDays = 7,
    decimal GrowthFactor = 1.0m) : IRequest<PurchaseForecastDto>;

public sealed class ForecastPurchasesValidator : AbstractValidator<ForecastPurchasesQuery>
{
    public ForecastPurchasesValidator()
    {
        RuleFor(x => x.HistoricalDays).InclusiveBetween(1, 365);
        RuleFor(x => x.TargetDays).InclusiveBetween(1, 60);
        RuleFor(x => x.GrowthFactor).InclusiveBetween(0.1m, 5m);
    }
}

public sealed class ForecastPurchasesHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<ForecastPurchasesQuery, PurchaseForecastDto>
{
    public async Task<PurchaseForecastDto> Handle(ForecastPurchasesQuery request, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var from = now.AddDays(-request.HistoricalDays);

        // Load delivered orders with their items in window.
        var deliveredOrders = await db.Orders
            .AsNoTracking()
            .Where(o => o.Status == OrderStatus.Delivered
                     && o.DeliveredAt != null
                     && o.DeliveredAt >= from
                     && o.DeliveredAt <= now)
            .Include(o => o.Items)
            .ToListAsync(ct).ConfigureAwait(false);

        // Preload full catalog graph (recipes + ingredients) for the calculator.
        var recipes = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Components)
            .ToListAsync(ct).ConfigureAwait(false);

        var ingredients = await db.Ingredients
            .AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);

        var calculator = new RecipeCostCalculator(ingredients, recipes);

        // Aggregate total consumption per ingredient across every delivered order item.
        var consumed = new Dictionary<Guid, decimal>();
        foreach (var order in deliveredOrders)
        {
            foreach (var item in order.Items)
            {
                // item.DishId references a recipe; skip if the recipe no longer exists (data drift).
                if (!recipes.Any(r => r.Id == item.DishId))
                    continue;

                IReadOnlyDictionary<Guid, decimal> perServing;
                try
                {
                    perServing = calculator.FlattenIngredients(item.DishId);
                }
                catch (DomainException)
                {
                    // Corrupted recipe graph — skip defensively, we don't want forecasting to crash.
                    continue;
                }

                foreach (var (ingredientId, qtyPerServing) in perServing)
                {
                    var total = qtyPerServing * item.Quantity;
                    consumed[ingredientId] = consumed.TryGetValue(ingredientId, out var prev)
                        ? prev + total
                        : total;
                }
            }
        }

        // Build DTO lines: one per ingredient with positive historical consumption OR below-reorder.
        var lines = new List<PurchaseForecastLineDto>();
        var historicalDays = request.HistoricalDays;
        var scale = (decimal)request.TargetDays / historicalDays * request.GrowthFactor;

        foreach (var ingredient in ingredients.Where(i => i.IsActive))
        {
            consumed.TryGetValue(ingredient.Id, out var historicalTotal);
            var dailyAvg = historicalTotal / historicalDays;
            var projected = historicalTotal * scale;

            // Suggested buy: cover the projection net of what we already have on hand,
            // and ensure we don't end the window below reorder_point.
            var afterProjection = ingredient.CurrentStockUseUnit - projected;
            var neededForReorder = Math.Max(0m, ingredient.ReorderPointUseUnit - afterProjection);
            var suggested = Math.Max(0m, projected - ingredient.CurrentStockUseUnit);
            suggested = Math.Max(suggested, neededForReorder);

            if (historicalTotal <= 0 && suggested <= 0)
                continue; // nothing to say about this ingredient this period

            var lastPurchasePrice = ingredient.Presentations
                .Where(p => p.IsActive && p.LastPurchasePriceUsd.HasValue)
                .OrderByDescending(p => p.UpdatedAt)
                .Select(p => p.LastPurchasePriceUsd)
                .FirstOrDefault();

            var estimatedCost = suggested > 0 && ingredient.AvgCostPerUseUnitUsd > 0
                ? (decimal?)(suggested * ingredient.AvgCostPerUseUnitUsd)
                : null;

            lines.Add(new PurchaseForecastLineDto(
                IngredientId: ingredient.Id,
                IngredientName: ingredient.Name,
                UseUnit: EnumDbMap<UseUnit>.ToDb(ingredient.UseUnit),
                HistoricalConsumedUseUnit: decimal.Round(historicalTotal, 4),
                DailyAverageUseUnit: decimal.Round(dailyAvg, 4),
                ProjectedUseUnit: decimal.Round(projected, 4),
                CurrentStockUseUnit: ingredient.CurrentStockUseUnit,
                ReorderPointUseUnit: ingredient.ReorderPointUseUnit,
                SuggestedPurchaseUseUnit: decimal.Round(suggested, 4),
                LastPurchasePriceUsd: lastPurchasePrice,
                AvgCostPerUseUnitUsd: ingredient.AvgCostPerUseUnitUsd > 0
                    ? ingredient.AvgCostPerUseUnitUsd : null,
                EstimatedCostUsd: estimatedCost is null ? null : decimal.Round(estimatedCost.Value, 4)));
        }

        // Sort by estimated cost desc (biggest spend first) to match the admin UI.
        lines = [.. lines.OrderByDescending(l => l.EstimatedCostUsd ?? 0m)
                         .ThenByDescending(l => l.SuggestedPurchaseUseUnit)];

        return new PurchaseForecastDto(
            HistoricalFrom: from,
            HistoricalTo: now,
            HistoricalDays: historicalDays,
            TargetDays: request.TargetDays,
            GrowthFactor: request.GrowthFactor,
            OrdersAnalyzed: deliveredOrders.Count,
            Lines: lines);
    }
}
