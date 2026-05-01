using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Delivery.Services;
using HomeChefPro.Domain.Delivery;
using HomeChefPro.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Delivery.Commands.IngestDeliveryEvent;

public sealed record IngestDeliveryEventCommand(
    string Provider,
    Guid? OrderId,
    string? ExternalTrackingId,
    string RawStatus,
    string RawPayloadJson,
    string? Signature = null,
    bool? SignatureValid = null,
    string? CourierName = null,
    string? CourierPhone = null,
    string? CourierVehicle = null,
    decimal? Lat = null,
    decimal? Lng = null,
    DateTimeOffset? EventAt = null) : IRequest<Guid>;

public sealed class IngestDeliveryEventValidator : AbstractValidator<IngestDeliveryEventCommand>
{
    public IngestDeliveryEventValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().MaximumLength(60);
        RuleFor(x => x.RawStatus).NotEmpty().MaximumLength(60);
        RuleFor(x => x.RawPayloadJson).NotEmpty();
        RuleFor(x => x).Must(x => x.OrderId.HasValue || !string.IsNullOrWhiteSpace(x.ExternalTrackingId))
            .WithMessage("Either OrderId or ExternalTrackingId must be provided.");
    }
}

public sealed class IngestDeliveryEventHandler(
    IHomeChefProDbContext db,
    TimeProvider clock)
    : IRequestHandler<IngestDeliveryEventCommand, Guid>
{
    public async Task<Guid> Handle(IngestDeliveryEventCommand request, CancellationToken ct)
    {
        var normalizedStatus = DeliveryStatusMap.Normalize(request.Provider, request.RawStatus);

        // Resolve the order by id OR by matching the external tracking on an existing tracking row.
        Order? order = null;
        DeliveryTracking? tracking = null;

        if (request.OrderId is { } oid)
        {
            order = await db.Orders.FirstOrDefaultAsync(o => o.Id == oid, ct).ConfigureAwait(false);
            tracking = await db.DeliveryTrackings.FirstOrDefaultAsync(t => t.OrderId == oid, ct)
                .ConfigureAwait(false);
        }
        else if (!string.IsNullOrWhiteSpace(request.ExternalTrackingId))
        {
            tracking = await db.DeliveryTrackings
                .FirstOrDefaultAsync(t =>
                    t.Provider == request.Provider
                    && t.ExternalTrackingId == request.ExternalTrackingId, ct)
                .ConfigureAwait(false);
            if (tracking is not null)
                order = await db.Orders.FirstOrDefaultAsync(o => o.Id == tracking.OrderId, ct)
                    .ConfigureAwait(false);
        }

        if (order is null)
            throw new NotFoundException(
                nameof(Order),
                request.OrderId?.ToString() ?? request.ExternalTrackingId ?? "(unknown)");

        // Record the immutable event.
        var evt = DeliveryEvent.Record(
            orderId: order.Id,
            provider: request.Provider,
            normalizedStatus: normalizedStatus,
            rawPayloadJson: request.RawPayloadJson,
            externalTrackingId: request.ExternalTrackingId,
            rawStatus: request.RawStatus,
            signature: request.Signature,
            signatureValid: request.SignatureValid,
            receivedAt: request.EventAt,
            clock: clock);
        db.DeliveryEvents.Add(evt);

        // Upsert the tracking projection.
        if (tracking is null)
        {
            tracking = DeliveryTracking.Assign(
                orderId: order.Id,
                provider: request.Provider,
                externalTrackingId: request.ExternalTrackingId,
                clock: clock);
            db.DeliveryTrackings.Add(tracking);
        }
        tracking.UpdateCourier(request.CourierName, request.CourierPhone, request.CourierVehicle, clock);
        tracking.ApplyEvent(normalizedStatus, request.Lat, request.Lng, request.EventAt, clock);

        // Project to the order FSM where it makes sense.
        if (normalizedStatus == DeliveryStatus.Delivered && order.Status == OrderStatus.InDelivery)
            order.MarkDelivered(clock);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return evt.Id;
    }
}
