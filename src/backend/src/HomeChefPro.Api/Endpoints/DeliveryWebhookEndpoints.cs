using System.Text.Json;
using HomeChefPro.Application.Delivery.Commands.IngestDeliveryEvent;
using MediatR;

namespace HomeChefPro.Api.Endpoints;

public static class DeliveryWebhookEndpoints
{
    private static readonly JsonSerializerOptions _jsonOptions =
        new() { PropertyNameCaseInsensitive = true };

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
            DeliveryWebhookSignatureVerifier verifier,
            IConfiguration config,
            ILoggerFactory logFactory,
            CancellationToken ct) =>
        {
            var log = logFactory.CreateLogger("DeliveryWebhook");
            request.EnableBuffering();

            using var reader = new StreamReader(request.Body, leaveOpen: true);
            var rawPayload = await reader.ReadToEndAsync(ct).ConfigureAwait(false);
            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(rawPayload))
                return Results.BadRequest(new { error = "Empty payload." });

            var signature = request.Headers.TryGetValue("X-Webhook-Signature", out var sig)
                ? sig.ToString() : null;

            // Verificacion HMAC. null = no hay secret configurado para este provider.
            // true/false = el secret existe y la firma matchea o no.
            var sigValid = verifier.Verify(provider, signature, rawPayload);

            // F-32 (audit Pasada B): fail-closed cuando sigValid != true.
            // Antes solo rechazabamos `false` (firma presente pero mala) y dejabamos pasar
            // `null` (secret vacio) como "no verificado". Eso permitia spoofing total cuando
            // un secret quedaba sin configurar en .env (default en deploy/.env.example).
            // Ahora ambos casos son rechazados cuando RejectInvalidSignature=true (default).
            if (sigValid != true)
            {
                var rejectInvalid = config.GetValue("DeliveryWebhooks:RejectInvalidSignature", true);
                var reason = sigValid == null
                    ? "no secret configured for provider"
                    : "signature does not match";
                if (rejectInvalid)
                {
                    log.LogWarning(
                        "Webhook REJECTED for provider={Provider}: {Reason}",
                        provider, reason);
                    return Results.Unauthorized();
                }
                // Modo permisivo (solo dev local): seguimos pero registramos la advertencia.
                log.LogWarning(
                    "Webhook accepted in PERMISSIVE mode for provider={Provider}: {Reason}",
                    provider, reason);
            }

            DeliveryWebhookPayload? parsed;
            try
            {
                parsed = JsonSerializer.Deserialize<DeliveryWebhookPayload>(rawPayload, _jsonOptions);
            }
            catch (JsonException ex)
            {
                return Results.BadRequest(new { error = "Invalid JSON", detail = ex.Message });
            }

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Status))
                return Results.BadRequest(new { error = "Missing 'status' field." });

            // Postgres `timestamptz` con Npgsql exige offset 0 (UTC). Los providers
            // suelen mandar eventAt en su zona local (ej. -04:00 Caracas) -> normalizamos.
            var eventAtUtc = parsed.EventAt?.ToUniversalTime();

            var id = await mediator.Send(new IngestDeliveryEventCommand(
                Provider: provider,
                OrderId: parsed.OrderId,
                ExternalTrackingId: parsed.ExternalTrackingId,
                RawStatus: parsed.Status,
                RawPayloadJson: rawPayload,
                Signature: signature,
                SignatureValid: sigValid,
                CourierName: parsed.CourierName,
                CourierPhone: parsed.CourierPhone,
                CourierVehicle: parsed.CourierVehicle,
                Lat: parsed.Lat,
                Lng: parsed.Lng,
                EventAt: eventAtUtc), ct);

            return Results.Accepted(value: new { eventId = id, signatureValid = sigValid });
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
