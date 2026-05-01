using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Inventory;

public sealed class IngredientWaste : AggregateRoot<Guid>
{
    public Guid IngredientId { get; private set; }
    public decimal QuantityUseUnit { get; private set; }
    public decimal EstimatedCostUsd { get; private set; }
    public WasteReason Reason { get; private set; }
    public string? Notes { get; private set; }
    public Guid RecordedBy { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    private IngredientWaste() { }

    private IngredientWaste(
        Guid id,
        Guid ingredientId,
        decimal quantityUseUnit,
        decimal estimatedCostUsd,
        WasteReason reason,
        string? notes,
        Guid recordedBy,
        DateTimeOffset recordedAt)
    {
        Id = id;
        IngredientId = ingredientId;
        QuantityUseUnit = quantityUseUnit;
        EstimatedCostUsd = estimatedCostUsd;
        Reason = reason;
        Notes = notes;
        RecordedBy = recordedBy;
        RecordedAt = recordedAt;
    }

    public static IngredientWaste Record(
        Guid ingredientId,
        decimal quantityUseUnit,
        WasteReason reason,
        Guid recordedBy,
        decimal estimatedCostUsd = 0m,
        string? notes = null,
        DateTimeOffset? recordedAt = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (ingredientId == Guid.Empty)
            throw new DomainException("IngredientId is required.");
        if (recordedBy == Guid.Empty)
            throw new DomainException("RecordedBy is required.");
        if (quantityUseUnit <= 0)
            throw new DomainException("Waste quantity must be positive.");
        if (estimatedCostUsd < 0)
            throw new DomainException("Estimated cost cannot be negative.");

        var when = recordedAt ?? (clock ?? TimeProvider.System).GetUtcNow();
        return new IngredientWaste(
            id ?? Guid.NewGuid(),
            ingredientId,
            quantityUseUnit,
            estimatedCostUsd,
            reason,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            recordedBy,
            when);
    }

    /// <summary>
    /// The DB trigger fills in <c>estimated_cost_usd</c> from ingredient avg cost if we sent 0.
    /// Infrastructure calls this after the insert to mirror DB value.
    /// </summary>
    internal void SyncEstimatedCostFromDatabase(decimal estimatedCostUsd) =>
        EstimatedCostUsd = estimatedCostUsd;
}
