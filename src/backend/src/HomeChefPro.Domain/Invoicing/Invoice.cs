using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Invoicing;

/// <summary>
/// Tax invoice for a delivered order. The fiscal numbers come from a SENIAT-authorized
/// provider (zcomp/hka/etc.); for development we ship a 'mock' provider that assigns
/// MOCK-{seq} numbers so the rest of the flow can be exercised without a real
/// machine. State machine: draft → issued → cancelled (or → failed if the provider
/// rejects).
/// </summary>
public sealed class Invoice : AggregateRoot<Guid>
{
    /// <summary>
    /// Pasada C / Fase 1C-A: tenant root. Default <c>Guid.Empty</c> (sentinel)
    /// hace que EF omita la columna en INSERT y la SQL DEFAULT inserte el
    /// piloto. Fase 2 reemplazara la sentinel por _currentChef.Id.
    /// </summary>
    public Guid ChefId { get; private set; }

    public Guid OrderId { get; private set; }

    public decimal SubtotalUsd { get; private set; }
    public decimal IvaUsd { get; private set; }
    public decimal IgtfUsd { get; private set; }
    public decimal TotalWithTaxUsd { get; private set; }
    public decimal IvaRate { get; private set; }
    public decimal IgtfRate { get; private set; }
    public bool IgtfApplies { get; private set; }

    public string? IssuerRif { get; private set; }
    public string? IssuerLegalName { get; private set; }
    public string? IssuerAddress { get; private set; }

    public string? CustomerRif { get; private set; }
    public string? CustomerLegalName { get; private set; }
    public string? CustomerAddress { get; private set; }

    public string Provider { get; private set; } = "mock";
    public string? FiscalNumber { get; private set; }
    public string? ControlNumber { get; private set; }
    public string? ProviderResponseJson { get; private set; }

    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;
    public DateTimeOffset? IssuedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public Guid? IssuedBy { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Invoice() { }

    private Invoice(
        Guid id,
        Guid orderId,
        decimal subtotalUsd,
        decimal ivaRate,
        decimal igtfRate,
        bool igtfApplies,
        string? issuerRif,
        string? issuerLegalName,
        string? issuerAddress,
        string? customerRif,
        string? customerLegalName,
        string? customerAddress,
        string provider,
        DateTimeOffset now)
    {
        Id = id;
        OrderId = orderId;
        SubtotalUsd = subtotalUsd;
        IvaRate = ivaRate;
        IgtfRate = igtfRate;
        IgtfApplies = igtfApplies;
        IvaUsd = decimal.Round(subtotalUsd * ivaRate, 4, MidpointRounding.AwayFromZero);
        IgtfUsd = igtfApplies
            ? decimal.Round(subtotalUsd * igtfRate, 4, MidpointRounding.AwayFromZero)
            : 0m;
        TotalWithTaxUsd = decimal.Round(subtotalUsd + IvaUsd + IgtfUsd, 4, MidpointRounding.AwayFromZero);
        IssuerRif = issuerRif;
        IssuerLegalName = issuerLegalName;
        IssuerAddress = issuerAddress;
        CustomerRif = customerRif;
        CustomerLegalName = customerLegalName;
        CustomerAddress = customerAddress;
        Provider = provider;
        Status = InvoiceStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public static Invoice CreateDraft(
        Guid orderId,
        decimal subtotalUsd,
        decimal ivaRate,
        decimal igtfRate,
        bool igtfApplies,
        string provider,
        string? issuerRif = null,
        string? issuerLegalName = null,
        string? issuerAddress = null,
        string? customerRif = null,
        string? customerLegalName = null,
        string? customerAddress = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");
        if (subtotalUsd < 0)
            throw new DomainException("Subtotal cannot be negative.");
        if (ivaRate < 0 || ivaRate >= 1)
            throw new DomainException("IVA rate must be between 0 and 1.");
        if (igtfRate < 0 || igtfRate >= 1)
            throw new DomainException("IGTF rate must be between 0 and 1.");
        if (string.IsNullOrWhiteSpace(provider))
            throw new DomainException("Provider is required.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new Invoice(
            id ?? Guid.NewGuid(),
            orderId,
            subtotalUsd,
            ivaRate,
            igtfRate,
            igtfApplies,
            issuerRif,
            issuerLegalName,
            issuerAddress,
            customerRif,
            customerLegalName,
            customerAddress,
            provider,
            now);
    }

    public void MarkIssued(
        string fiscalNumber,
        string controlNumber,
        Guid issuedBy,
        string? providerResponseJson = null,
        TimeProvider? clock = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException(
                $"Cannot issue invoice from status '{Status}'.");
        if (string.IsNullOrWhiteSpace(fiscalNumber))
            throw new DomainException("Fiscal number is required.");
        if (string.IsNullOrWhiteSpace(controlNumber))
            throw new DomainException("Control number is required.");
        if (issuedBy == Guid.Empty)
            throw new DomainException("IssuedBy is required.");

        FiscalNumber = fiscalNumber;
        ControlNumber = controlNumber;
        IssuedBy = issuedBy;
        ProviderResponseJson = providerResponseJson;
        Status = InvoiceStatus.Issued;
        IssuedAt = (clock ?? TimeProvider.System).GetUtcNow();
        UpdatedAt = IssuedAt.Value;
    }

    public void MarkFailed(string providerResponseJson, TimeProvider? clock = null)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException(
                $"Cannot mark failed from status '{Status}'.");
        ProviderResponseJson = providerResponseJson;
        Status = InvoiceStatus.Failed;
        UpdatedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public void Cancel(string reason, TimeProvider? clock = null)
    {
        if (Status != InvoiceStatus.Issued)
            throw new DomainException(
                $"Only issued invoices can be cancelled. Current status: '{Status}'.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Cancellation reason is required.");
        Status = InvoiceStatus.Cancelled;
        CancellationReason = reason.Trim();
        CancelledAt = (clock ?? TimeProvider.System).GetUtcNow();
        UpdatedAt = CancelledAt.Value;
    }
}
