using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Payments.Commands.RejectPayment;

public sealed record RejectPaymentCommand(Guid PaymentId, string Reason) : IRequest;

public sealed class RejectPaymentValidator : AbstractValidator<RejectPaymentCommand>
{
    public RejectPaymentValidator()
    {
        RuleFor(x => x.PaymentId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(4000);
    }
}

public sealed class RejectPaymentHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    INotificationService notifications, // Etapa 5
    TimeProvider clock)
    : IRequestHandler<RejectPaymentCommand>
{
    public async Task Handle(RejectPaymentCommand request, CancellationToken ct)
    {
        var payment = await db.Payments.FindAsync([request.PaymentId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        payment.Reject(request.Reason, currentUser.RequireUserId(), clock);

        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), payment.OrderId);

        if (order.Status is OrderStatus.PendingPayment or OrderStatus.PaymentVerifying)
            order.RejectPayment(request.Reason, clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Etapa 5: notificar al cliente — best-effort.
        await notifications.NotifyOrderAsync(
            order.Id,
            "Problema con tu pago",
            "Hubo un inconveniente con tu comprobante. Por favor revisa tu pedido.",
            ct).ConfigureAwait(false);
    }
}
