using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Payments;

public sealed class Payment : AggregateRoot<Guid>
{
    public Guid OrderId { get; private set; }
    public PaymentMethod Method { get; private set; }

    public decimal AmountUsd { get; private set; }
    public PaidCurrency PaidCurrency { get; private set; }
    public decimal AmountPaidCurrency { get; private set; }
    public decimal? ExchangeRateUsed { get; private set; }

    public string? ReferenceNumber { get; private set; }
    public string? ProofImageUrl { get; private set; }
    public string? PayerName { get; private set; }
    public string? PayerPhone { get; private set; }
    public string? PayerAccountLast4 { get; private set; }

    public PaymentStatus Status { get; private set; }
    public Guid? VerifiedBy { get; private set; }
    public DateTimeOffset? VerifiedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private Payment() { }

    private Payment(
        Guid id,
        Guid orderId,
        PaymentMethod method,
        decimal amountUsd,
        PaidCurrency paidCurrency,
        decimal amountPaidCurrency,
        decimal? exchangeRateUsed,
        string? referenceNumber,
        string? proofImageUrl,
        string? payerName,
        string? payerPhone,
        string? payerAccountLast4,
        DateTimeOffset createdAt)
    {
        Id = id;
        OrderId = orderId;
        Method = method;
        AmountUsd = amountUsd;
        PaidCurrency = paidCurrency;
        AmountPaidCurrency = amountPaidCurrency;
        ExchangeRateUsed = exchangeRateUsed;
        ReferenceNumber = referenceNumber;
        ProofImageUrl = proofImageUrl;
        PayerName = payerName;
        PayerPhone = payerPhone;
        PayerAccountLast4 = payerAccountLast4;
        Status = PaymentStatus.Pending;
        CreatedAt = createdAt;
    }

    public static Payment Submit(
        Guid orderId,
        PaymentMethod method,
        decimal amountUsd,
        PaidCurrency paidCurrency,
        decimal amountPaidCurrency,
        decimal? exchangeRateUsed = null,
        string? referenceNumber = null,
        string? proofImageUrl = null,
        string? payerName = null,
        string? payerPhone = null,
        string? payerAccountLast4 = null,
        TimeProvider? clock = null,
        Guid? id = null)
    {
        if (orderId == Guid.Empty)
            throw new DomainException("OrderId is required.");
        if (amountUsd <= 0)
            throw new DomainException("AmountUsd must be positive.");
        if (amountPaidCurrency <= 0)
            throw new DomainException("AmountPaidCurrency must be positive.");
        if (paidCurrency == PaidCurrency.Ves && exchangeRateUsed is null)
            throw new DomainException("VES payments require an exchange rate.");
        if (exchangeRateUsed is <= 0)
            throw new DomainException("Exchange rate used must be positive.");

        var now = (clock ?? TimeProvider.System).GetUtcNow();
        return new Payment(
            id ?? Guid.NewGuid(),
            orderId,
            method,
            amountUsd,
            paidCurrency,
            amountPaidCurrency,
            exchangeRateUsed,
            TrimOrNull(referenceNumber),
            TrimOrNull(proofImageUrl),
            TrimOrNull(payerName),
            TrimOrNull(payerPhone),
            TrimOrNull(payerAccountLast4),
            now);
    }

    public void Verify(Guid verifiedBy, TimeProvider? clock = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(
                $"Payment {Id} cannot be verified from status '{Status}'.");
        if (verifiedBy == Guid.Empty)
            throw new DomainException("VerifiedBy is required.");
        Status = PaymentStatus.Verified;
        VerifiedBy = verifiedBy;
        VerifiedAt = (clock ?? TimeProvider.System).GetUtcNow();
        RejectionReason = null;
    }

    public void Reject(string reason, Guid rejectedBy, TimeProvider? clock = null)
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(
                $"Payment {Id} cannot be rejected from status '{Status}'.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Rejection reason is required.");
        if (rejectedBy == Guid.Empty)
            throw new DomainException("RejectedBy is required.");
        Status = PaymentStatus.Rejected;
        RejectionReason = reason.Trim();
        VerifiedBy = rejectedBy;
        VerifiedAt = (clock ?? TimeProvider.System).GetUtcNow();
    }

    public void AttachProofImage(string proofImageUrl)
    {
        if (string.IsNullOrWhiteSpace(proofImageUrl))
            throw new DomainException("Proof image URL is required.");
        ProofImageUrl = proofImageUrl.Trim();
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
