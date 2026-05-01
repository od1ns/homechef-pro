namespace HomeChefPro.Application.Invoicing.Abstractions;

/// <summary>
/// Abstraction over a SENIAT-authorized fiscal printer or web service.
/// The default <see cref="MockFiscalProvider"/> assigns MOCK-{seq}/CTRL-{seq} numbers
/// so the rest of the pipeline works during development. Plug a real provider
/// (Z-Comp, HKA, etc.) by replacing the registration in DI.
/// </summary>
public interface IFiscalProvider
{
    string ProviderName { get; }

    Task<FiscalEmissionResult> EmitAsync(
        FiscalEmissionRequest request,
        CancellationToken ct = default);
}

public sealed record FiscalEmissionRequest(
    Guid InvoiceId,
    Guid OrderId,
    string OrderNumber,
    decimal SubtotalUsd,
    decimal IvaUsd,
    decimal IgtfUsd,
    decimal TotalWithTaxUsd,
    string? IssuerRif,
    string? IssuerLegalName,
    string? CustomerRif,
    string? CustomerLegalName,
    IReadOnlyList<FiscalLine> Lines);

public sealed record FiscalLine(
    string DishName,
    int Quantity,
    decimal UnitPriceUsd,
    decimal LineTotalUsd);

public sealed record FiscalEmissionResult(
    bool Succeeded,
    string? FiscalNumber,
    string? ControlNumber,
    string? RawResponseJson,
    string? FailureReason);
