using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Exchange;

public sealed class ExchangeRate : AggregateRoot<Guid>
{
    /// <summary>
    /// Pasada C / Fase 1C-A: tenant root. Default <c>Guid.Empty</c> (sentinel)
    /// hace que EF omita la columna en INSERT y la SQL DEFAULT inserte el
    /// piloto. Fase 2 reemplazara la sentinel por _currentChef.Id.
    /// </summary>
    public Guid ChefId { get; private set; }

    public decimal RateVesPerUsd { get; private set; }
    public DateOnly EffectiveDate { get; private set; }
    public Guid SetBy { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ExchangeRate() { }

    private ExchangeRate(
        Guid id,
        decimal rateVesPerUsd,
        DateOnly effectiveDate,
        Guid setBy,
        string? notes,
        DateTimeOffset createdAt)
    {
        Id = id;
        RateVesPerUsd = rateVesPerUsd;
        EffectiveDate = effectiveDate;
        SetBy = setBy;
        Notes = notes;
        CreatedAt = createdAt;
    }

    public static ExchangeRate Publish(
        decimal rateVesPerUsd,
        DateOnly effectiveDate,
        Guid setBy,
        string? notes = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (rateVesPerUsd <= 0)
            throw new DomainException("Exchange rate must be positive.");
        if (setBy == Guid.Empty)
            throw new DomainException("SetBy is required.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new ExchangeRate(
            id ?? Guid.NewGuid(),
            rateVesPerUsd,
            effectiveDate,
            setBy,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            now);
    }

    public void UpdateRate(decimal rateVesPerUsd, Guid setBy, string? notes = null)
    {
        if (rateVesPerUsd <= 0)
            throw new DomainException("Exchange rate must be positive.");
        if (setBy == Guid.Empty)
            throw new DomainException("SetBy is required.");
        RateVesPerUsd = rateVesPerUsd;
        SetBy = setBy;
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
    }

    public decimal ConvertUsdToVes(decimal amountUsd) => amountUsd * RateVesPerUsd;
    public decimal ConvertVesToUsd(decimal amountVes) => amountVes / RateVesPerUsd;
}
