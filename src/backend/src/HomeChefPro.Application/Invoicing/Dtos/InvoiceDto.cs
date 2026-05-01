namespace HomeChefPro.Application.Invoicing.Dtos;

public sealed record InvoiceDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    string Status,                 // 'draft' | 'issued' | 'cancelled' | 'failed'
    decimal SubtotalUsd,
    decimal IvaUsd,
    decimal IgtfUsd,
    decimal TotalWithTaxUsd,
    bool IgtfApplies,
    string Provider,
    string? FiscalNumber,
    string? ControlNumber,
    string? IssuerRif,
    string? IssuerLegalName,
    string? CustomerRif,
    string? CustomerLegalName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? CancelledAt,
    string? CancellationReason);
