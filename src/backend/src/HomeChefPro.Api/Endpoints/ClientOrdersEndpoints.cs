using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Orders.Queries.GetOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Application.Receipts.Queries.GetOrderReceipt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class ClientOrdersEndpoints
{
    /// <summary>
    /// F-24 (audit Pasada B): el token anti-IDOR se acepta en el query param
    /// <c>?token=...</c> O en el header <c>X-Order-Token</c>. Si no llega ninguno,
    /// el handler trata el lookup como "sin token" y devuelve 404 si el order existe
    /// (los endpoints client siempre exigen token; los admin no).
    /// </summary>
    private const string OrderTokenHeader = "X-Order-Token";

    private static string? ExtractAccessToken(HttpRequest req)
    {
        var fromQuery = req.Query["token"].ToString();
        if (!string.IsNullOrEmpty(fromQuery)) return fromQuery;
        if (req.Headers.TryGetValue(OrderTokenHeader, out var hdr))
        {
            var fromHeader = hdr.ToString();
            if (!string.IsNullOrEmpty(fromHeader)) return fromHeader;
        }
        return null;
    }

    public static IEndpointRouteBuilder MapClientOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/client/orders")
            .WithTags("Client: Orders")
            .AllowAnonymous();

        // POST /api/client/orders — crea el order, retorna { id, accessToken }.
        // El cliente DEBE persistir el accessToken; sin el no podra consultar el order.
        group.MapPost("", async (
            [FromBody] CreateGuestOrderCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var result = await mediator.Send(cmd, ct);
            return Results.Created(
                $"/api/client/orders/{result.Id}",
                new { id = result.Id, accessToken = result.AccessToken });
        });

        // GET /api/client/orders/{id} — exige ?token=... o X-Order-Token header.
        // F-24: sin token devuelve 404 (no 401) para no revelar la existencia del order.
        group.MapGet("{id:guid}", async (
            Guid id,
            HttpRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var token = ExtractAccessToken(req);
            if (string.IsNullOrEmpty(token))
                return Results.NotFound();
            var dto = await mediator.Send(new GetOrderQuery(id, token), ct);
            return Results.Ok(dto);
        });

        // GET /api/client/orders/{id}/receipt.pdf — idem token requerido.
        // El receipt incluye RIF + items + total + datos del cliente; F-24 lo bloquea
        // a quien no tenga el token.
        group.MapGet("{id:guid}/receipt.pdf", async (
            Guid id,
            HttpRequest req,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var token = ExtractAccessToken(req);
            if (string.IsNullOrEmpty(token))
                return Results.NotFound();
            // Validacion previa: si el token no matchea el order, GetOrderQuery tira 404.
            // Asi evitamos generar el PDF para alguien sin permiso.
            _ = await mediator.Send(new GetOrderQuery(id, token), ct);
            var pdf = await mediator.Send(new GetOrderReceiptQuery(id), ct);
            return Results.File(pdf.Pdf, pdf.ContentType, pdf.FileName);
        });

        group.MapPost("{id:guid}/payment", async (
            Guid id,
            [FromBody] SubmitPaymentRequest body,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var cmd = new SubmitPaymentProofCommand(
                OrderId: id,
                Method: body.Method,
                AmountUsd: body.AmountUsd,
                PaidCurrency: body.PaidCurrency,
                AmountPaidCurrency: body.AmountPaidCurrency,
                ExchangeRateUsed: body.ExchangeRateUsed,
                ReferenceNumber: body.ReferenceNumber,
                ProofImageId: body.ProofImageId,
                PayerName: body.PayerName,
                PayerPhone: body.PayerPhone,
                PayerAccountLast4: body.PayerAccountLast4);
            var paymentId = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId(
                $"/api/client/orders/{id}/payment/{paymentId}", paymentId);
        });

        return app;
    }

    public sealed record SubmitPaymentRequest(
        string Method,
        decimal AmountUsd,
        string PaidCurrency,
        decimal AmountPaidCurrency,
        decimal? ExchangeRateUsed = null,
        string? ReferenceNumber = null,
        Guid? ProofImageId = null,
        string? PayerName = null,
        string? PayerPhone = null,
        string? PayerAccountLast4 = null);
}
