using HomeChefPro.Application.Orders.Commands.CreateGuestOrder;
using HomeChefPro.Application.Orders.Queries.GetOrder;
using HomeChefPro.Application.Payments.Commands.SubmitPaymentProof;
using HomeChefPro.Application.Receipts.Queries.GetOrderReceipt;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HomeChefPro.Api.Endpoints;

public static class ClientOrdersEndpoints
{
    public static IEndpointRouteBuilder MapClientOrdersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/client/orders")
            .WithTags("Client: Orders")
            .AllowAnonymous();

        group.MapPost("", async (
            [FromBody] CreateGuestOrderCommand cmd,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var id = await mediator.Send(cmd, ct);
            return EndpointResults.CreatedId($"/api/client/orders/{id}", id);
        });

        // Tracking endpoint — returns order detail by id. In a real rollout this should
        // require a light proof (matching phone / short token). For now it's by id.
        group.MapGet("{id:guid}", async (Guid id, IMediator mediator, CancellationToken ct) =>
            Results.Ok(await mediator.Send(new GetOrderQuery(id), ct)));

        group.MapGet("{id:guid}/receipt.pdf", async (
            Guid id, IMediator mediator, CancellationToken ct) =>
        {
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
                ProofImageUrl: body.ProofImageUrl,
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
        string? ProofImageUrl = null,
        string? PayerName = null,
        string? PayerPhone = null,
        string? PayerAccountLast4 = null);
}
