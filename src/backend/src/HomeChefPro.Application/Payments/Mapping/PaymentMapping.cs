using HomeChefPro.Application.Payments.Dtos;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Payments;

namespace HomeChefPro.Application.Payments.Mapping;

public static class PaymentMapping
{
    public static PaymentDto ToDto(this Payment p) =>
        new(
            Id: p.Id,
            OrderId: p.OrderId,
            Method: EnumDbMap<PaymentMethod>.ToDb(p.Method),
            AmountUsd: p.AmountUsd,
            PaidCurrency: EnumDbMap<PaidCurrency>.ToDb(p.PaidCurrency),
            AmountPaidCurrency: p.AmountPaidCurrency,
            ExchangeRateUsed: p.ExchangeRateUsed,
            ReferenceNumber: p.ReferenceNumber,
            ProofImageUrl: p.ProofImageUrl,
            PayerName: p.PayerName,
            PayerPhone: p.PayerPhone,
            PayerAccountLast4: p.PayerAccountLast4,
            Status: EnumDbMap<PaymentStatus>.ToDb(p.Status),
            VerifiedBy: p.VerifiedBy,
            VerifiedAt: p.VerifiedAt,
            RejectionReason: p.RejectionReason,
            CreatedAt: p.CreatedAt);
}
