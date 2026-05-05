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
    Guid? ProofImageId = null,
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
    TimeProvider clock,
    HomeChefPro.Application.Uploads.Abstractions.IUploadUrlBuilder urlBuilder)
    : IRequestHandler<SubmitPaymentProofCommand, Guid>
{
    public async Task<Guid> Handle(SubmitPaymentProofCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        // F-25 (audit Pasada B): rechazar re-submit si el order no esta en PendingPayment.
        // Antes el handler creaba un Payment record duplicado y solo "no avanzaba el state",
        // dejando registros zombi en la BD y posibles re-aprobaciones por error del admin.
        if (order.Status != OrderStatus.PendingPayment)
        {
            throw new HomeChefPro.Domain.Common.DomainException(
                $"Order is in state '{order.Status}'; cannot accept new payment proof. " +
                "Only orders in 'pending_payment' accept new submissions.");
        }

        // F-22 (audit Pasada B): el AmountUsd declarado debe coincidir con order.TotalUsd.
        // Tolerancia 0.01 USD para evitar issues de precision decimal en multi-currency.
        // Antes: el cliente podia declarar AmountUsd: 0.01 para un order de 50 USD y
        // depender de que el admin lo notara manualmente.
        if (Math.Abs(request.AmountUsd - order.TotalUsd) > 0.01m)
        {
            throw new HomeChefPro.Domain.Common.DomainException(
                $"Payment amount ({request.AmountUsd:F2} USD) does not match order total " +
                $"({order.TotalUsd:F2} USD).");
        }

        // F-27 (audit Pasada B): validar coherencia entre AmountUsd, AmountPaidCurrency y
        // ExchangeRateUsed cuando se paga en VES. Evita combinaciones absurdas tipo
        // "AmountUsd: 50, AmountPaidCurrency: 1, ExchangeRateUsed: 50" que pasaban antes.
        if (request.PaidCurrency == "VES" && request.ExchangeRateUsed is decimal rate)
        {
            var derivedUsd = request.AmountPaidCurrency / rate;
            if (Math.Abs(derivedUsd - request.AmountUsd) > 0.05m)
            {
                throw new HomeChefPro.Domain.Common.DomainException(
                    $"Inconsistent VES payment: {request.AmountPaidCurrency:F2} VES at rate " +
                    $"{rate:F4} VES/USD = {derivedUsd:F2} USD, but AmountUsd declared as " +
                    $"{request.AmountUsd:F2} USD. Difference must be <= 0.05 USD.");
            }
        }

        // F-23: si el cliente envia ProofImageId, lookup del upload + validar no-claimed.
        // Construimos la URL canonica desde el filename + PublicBaseUrl. Esto reemplaza
        // ProofImageUrl libre, que permitia URLs externas / reuse de comprobantes.
        string? proofImageUrl = null;
        HomeChefPro.Domain.Payments.PaymentProofUpload? upload = null;
        if (request.ProofImageId is { } proofId)
        {
            upload = await db.PaymentProofUploads
                .FirstOrDefaultAsync(u => u.Id == proofId, ct)
                .ConfigureAwait(false)
                ?? throw new NotFoundException("PaymentProofUpload", proofId);
            if (upload.ClaimedAt is not null)
            {
                throw new HomeChefPro.Domain.Common.DomainException(
                    $"Upload {proofId} is already associated with payment " +
                    $"{upload.ClaimedByPaymentId}; cannot reuse for another payment.");
            }
            // Pasada C / H-05: el url se construye con el chef del upload.
            proofImageUrl = urlBuilder.BuildPaymentProofUrl(upload.ChefId, upload.Filename);
        }

        var payment = Payment.Submit(
            orderId: request.OrderId,
            method: EnumDbMap<PaymentMethod>.FromDb(request.Method),
            amountUsd: request.AmountUsd,
            paidCurrency: EnumDbMap<PaidCurrency>.FromDb(request.PaidCurrency),
            amountPaidCurrency: request.AmountPaidCurrency,
            exchangeRateUsed: request.ExchangeRateUsed,
            referenceNumber: request.ReferenceNumber,
            proofImageUrl: proofImageUrl,
            payerName: request.PayerName,
            payerPhone: request.PayerPhone,
            payerAccountLast4: request.PayerAccountLast4,
            clock: clock);
        db.Payments.Add(payment);

        // Bug fix Fase 6-B: el FK payment_proof_uploads.claimed_by_payment_id apunta
        // a payments(id), pero la relacion no esta modelada en EF (solo en SQL).
        // Si guardamos todo en un SaveChanges, EF puede mandar el UPDATE del upload
        // ANTES del INSERT del payment, violando el FK.
        // Fix: insert del payment primero, despues claim del upload + advance del
        // order en un segundo SaveChanges.
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // F-23: claim el upload despues de crear el Payment (one-shot use).
        upload?.Claim(payment.Id, clock);

        // Move the order to payment_verifying so the admin sees it in their queue.
        order.SubmitPayment(clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return payment.Id;
    }
}
