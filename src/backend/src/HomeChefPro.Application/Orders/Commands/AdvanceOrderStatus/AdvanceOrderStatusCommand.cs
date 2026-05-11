using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Orders.Commands.AdvanceOrderStatus;

/// <summary>
/// Single handler for all admin/kitchen FSM transitions. Keeps the API surface small
/// and routes to the right domain method based on the requested target.
/// </summary>
public sealed record AdvanceOrderStatusCommand(
    Guid OrderId,
    string Target,               // "payment_verifying" | "paid" | "in_preparation" | "ready" | "in_delivery" | "delivered" | "rejected" | "cancelled"
    string? Reason = null         // required for rejected / cancelled
) : IRequest;

public sealed class AdvanceOrderStatusValidator : AbstractValidator<AdvanceOrderStatusCommand>
{
    private static readonly HashSet<string> Allowed =
    [
        "payment_verifying", "paid", "in_preparation", "ready",
        "in_delivery", "delivered", "rejected", "cancelled",
    ];

    public AdvanceOrderStatusValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Target).NotEmpty()
            .Must(t => Allowed.Contains(t))
            .WithMessage(x => $"Target '{x.Target}' is not a valid transition target.");
        When(x => x.Target is "rejected" or "cancelled", () =>
        {
            RuleFor(x => x.Reason).NotEmpty()
                .WithMessage("Reason is required when rejecting or cancelling.");
        });
    }
}

public sealed class AdvanceOrderStatusHandler(
    IHomeChefProDbContext db,
    INotificationService notifications, // Etapa 5
    TimeProvider clock)
    : IRequestHandler<AdvanceOrderStatusCommand>
{
    public async Task Handle(AdvanceOrderStatusCommand request, CancellationToken ct)
    {
        var order = await db.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        switch (request.Target)
        {
            case "payment_verifying": order.SubmitPayment(clock); break;
            case "paid":               order.ApprovePayment(clock); break;
            case "in_preparation":     order.StartPreparation(clock); break;
            case "ready":              order.MarkReady(clock); break;
            case "in_delivery":        order.DispatchForDelivery(clock); break;
            case "delivered":          order.MarkDelivered(clock); break;
            case "rejected":           order.RejectPayment(request.Reason!, clock); break;
            case "cancelled":          order.Cancel(request.Reason!, clock); break;
            default:
                throw new InvalidOperationException($"Unknown target: {request.Target}");
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Etapa 5: push según el estado nuevo — best-effort.
        var (title, body) = request.Target switch
        {
            "ready"       => ("Tu pedido está listo", "Pasa a retirar tu pedido cuando quieras."),
            "in_delivery" => ("Tu pedido está en camino", "El repartidor ya va hacia ti."),
            "delivered"   => ("Pedido entregado", "¡Buen provecho! Puedes dejar tu opinión en la app."),
            _             => (null, null),
        };
        if (title is not null)
            await notifications.NotifyOrderAsync(order.Id, title, body!, ct).ConfigureAwait(false);
    }
}
