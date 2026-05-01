namespace HomeChefPro.Application.Payments.Dtos;

public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    string Method,
    decimal AmountUsd,
    string PaidCurrency,
    decimal AmountPaidCurrency,
    decimal? ExchangeRateUsed,
    string? ReferenceNumber,
    string? ProofImageUrl,
    string? PayerName,
    string? PayerPhone,
    string? PayerAccountLast4,
    string Status,
    Guid? VerifiedBy,
    DateTimeOffset? VerifiedAt,
    string? RejectionReason,
    DateTimeOffset CreatedAt);
