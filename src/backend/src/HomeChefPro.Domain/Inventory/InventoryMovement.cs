using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Inventory;

/// <summary>
/// Audit log entity populated by DB triggers. Domain reads only — it does not insert movements
/// directly; the SQL triggers (<c>fn_apply_purchase_to_stock</c>, <c>fn_apply_waste_to_stock</c>, etc.)
/// are the writers. This class exists so repositories can return movements for reports.
/// </summary>
public sealed class InventoryMovement : Entity<Guid>
{
    public Guid IngredientId { get; private set; }
    public MovementType MovementType { get; private set; }
    public decimal QuantityUseUnit { get; private set; }
    public decimal CostImpactUsd { get; private set; }
    public string SourceTable { get; private set; } = null!;
    public Guid? SourceId { get; private set; }
    public decimal ResultingStock { get; private set; }
    public decimal ResultingAvgCost { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public string? Notes { get; private set; }

    private InventoryMovement() { }

    /// <summary>
    /// Used by Infrastructure when hydrating rows from the <c>inventory_movements</c> table.
    /// Not intended to be used by Application code for writes.
    /// </summary>
    internal static InventoryMovement Hydrate(
        Guid id,
        Guid ingredientId,
        MovementType movementType,
        decimal quantityUseUnit,
        decimal costImpactUsd,
        string sourceTable,
        Guid? sourceId,
        decimal resultingStock,
        decimal resultingAvgCost,
        DateTimeOffset occurredAt,
        string? notes) =>
        new()
        {
            Id = id,
            IngredientId = ingredientId,
            MovementType = movementType,
            QuantityUseUnit = quantityUseUnit,
            CostImpactUsd = costImpactUsd,
            SourceTable = sourceTable,
            SourceId = sourceId,
            ResultingStock = resultingStock,
            ResultingAvgCost = resultingAvgCost,
            OccurredAt = occurredAt,
            Notes = notes,
        };
}
