using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;

public sealed record SubmitPaymentProofCommand(
    Guid OrderId,
    string Method,
    decimal AmountUsd,
    string PaidCurrency,
    decimal AmountPaidCurrency,
    decimal? ExchangeRateUsed = null,
    string? ReferenceNumber = null,
    string? ProofImageUrl = null,
    string? PayerName = null,
    string? PayerPhone = null,
    string? PayerAccountLast4 = null) : IRequest<Guid>;

public sealed class SubmitPaymentProofValidator : AbstractValidator<SubmitPaymentProofCommand>
{
    public SubmitPaymentProofValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Method).NotEmpty()
            .Must(m => EnumDbMap<PaymentMethod>.TryFromDb(m, out _))
            .WithMessage("Unknown payment method.");
        RuleFor(x => x.AmountUsd).GreaterThan(0);
        RuleFor(x => x.PaidCurrency).NotEmpty()
            .Must(c => EnumDbMap<PaidCurrency>.TryFromDb(c, out _))
            .WithMessage("PaidCurrency must be 'USD' or 'VES'.");
        RuleFor(x => x.AmountPaidCurrency).GreaterThan(0);
        When(x => x.PaidCurrency == "VES", () =>
        {
            RuleFor(x => x.ExchangeRateUsed).NotNull().GreaterThan(0)
                .WithMessage("Exchange rate is required for VES payments.");
        });
        RuleFor(x => x.ReferenceNumber).MaximumLength(80);
        RuleFor(x => x.PayerName).MaximumLength(160);
        RuleFor(x => x.PayerPhone).MaximumLength(30);
        RuleFor(x => x.PayerAccountLast4).MaximumLength(10);
    }
}

public sealed class SubmitPaymentProofHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<SubmitPaymentProofCommand, Guid>
{
    public async Task<Guid> Handle(SubmitPaymentProofCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var payment = Payment.Submit(
            orderId: request.OrderId,
            method: EnumDbMap<PaymentMethod>.FromDb(request.Method),
            amountUsd: request.AmountUsd,
            paidCurrency: EnumDbMap<PaidCurrency>.FromDb(request.PaidCurrency),
            amountPaidCurrency: request.AmountPaidCurrency,
            exchangeRateUsed: request.ExchangeRateUsed,
            referenceNumber: request.ReferenceNumber,
            proofImageUrl: request.ProofImageUrl,
            payerName: request.PayerName,
            payerPhone: request.PayerPhone,
            payerAccountLast4: request.PayerAccountLast4,
            clock: clock);
        db.Payments.Add(payment);

        // Move the order to payment_verifying so the admin sees it in their queue.
        if (order.Status == OrderStatus.PendingPayment)
            order.SubmitPayment(clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return payment.Id;
    }
}
