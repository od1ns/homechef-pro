using System.Text.Json;
using HomeChefPro.Application.Delivery.Commands.IngestDeliveryEvent;
using MediatR;

namespace HomeChefPro.Api.Endpoints;

public static class DeliveryWebhookEndpoints
{
    public static IEndpointRouteBuilder MapDeliveryWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/webhooks/delivery")
            .WithTags("Webhooks: Delivery")
            .AllowAnonymous();

        // Providers post here with their own JSON shape; we parse defensively.
        group.MapPost("{provider}", async (
            string provider,
            HttpRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var rawPayload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(rawPayload))
                return Results.BadRequest(new { error = "Empty payload." });

            DeliveryWebhookPayload? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<DeliveryWebhookPayload>(rawPayload,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = "Invalid JSON", detail = ex.Message });
            }

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Status))
                return Results.BadRequest(new { error = "Missing 'status' field." });

            var signature = request.Headers.TryGetValue("X-Webhook-Signature", out var sig)
                ? sig.ToString() : null;

            var id = await mediator.Send(new IngestDeliveryEventCommand(
                Provider: provider,
                OrderId: parsed.OrderId,
                ExternalTrackingId: parsed.ExternalTrackingId,
                RawStatus: parsed.Status,
                RawPayloadJson: rawPayload,
                Signature: signature,
                SignatureValid: null,                 // TODO: per-provider HMAC verify when we wire secrets
                CourierName: parsed.CourierName,
                CourierPhone: parsed.CourierPhone,
                CourierVehicle: parsed.CourierVehicle,
                Lat: parsed.Lat,
                Lng: parsed.Lng,
                EventAt: parsed.EventAt), ct);

            return Results.Accepted(value: new { eventId = id });
        });

        return app;
    }

    public sealed record DeliveryWebhookPayload(
        string Status,
        Guid? OrderId = null,
        string? ExternalTrackingId = null,
        string? CourierName = null,
        string? CourierPhone = null,
        string? CourierVehicle = null,
        decimal? Lat = null,
        decimal? Lng = null,
        DateTimeOffset? EventAt = null);
}
