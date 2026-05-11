using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Payments.Commands.VerifyPayment;

public sealed record VerifyPaymentCommand(Guid PaymentId) : IRequest;

public sealed class VerifyPaymentValidator : AbstractValidator<VerifyPaymentCommand>
{
    public VerifyPaymentValidator() => RuleFor(x => x.PaymentId).NotEmpty();
}

public sealed class VerifyPaymentHandler(
    IHomeChefProDbContext db,
    ICurrentUser currentUser,
    INotificationService notifications, // Etapa 5
    TimeProvider clock)
    : IRequestHandler<VerifyPaymentCommand>
{
    public async Task Handle(VerifyPaymentCommand request, CancellationToken ct)
    {
        var payment = await db.Payments.FindAsync([request.PaymentId], ct).ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Payment), request.PaymentId);

        payment.Verify(currentUser.RequireUserId(), clock);

        // Approving the payment should move the order to 'paid' (only valid from payment_verifying).
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), payment.OrderId);

        if (order.Status == OrderStatus.PaymentVerifying)
            order.ApprovePayment(clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Etapa 5: notificar al cliente — best-effort, nunca bloquea la respuesta.
        await notifications.NotifyOrderAsync(
            order.Id,
            "Pago confirmado",
            "Tu pedido fue confirmado. ¡Estamos preparándolo!",
            ct).ConfigureAwait(false);
    }
}
