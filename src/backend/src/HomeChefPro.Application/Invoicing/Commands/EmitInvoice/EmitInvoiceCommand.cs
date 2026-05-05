using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Invoicing.Abstractions;
using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Application.Invoicing.Mapping;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Invoicing;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Invoicing.Commands.EmitInvoice;

public sealed record EmitInvoiceCommand(
    Guid OrderId,
    string? CustomerRif = null,
    string? CustomerLegalName = null,
    string? CustomerAddress = null) : IRequest<InvoiceDto>;

public sealed class EmitInvoiceValidator : AbstractValidator<EmitInvoiceCommand>
{
    public EmitInvoiceValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.CustomerRif).MaximumLength(20);
        RuleFor(x => x.CustomerLegalName).MaximumLength(200);
    }
}

public sealed class EmitInvoiceHandler(
    IHomeChefProDbContext db,
    IFiscalProvider provider,
    InvoicingSettings settings,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<EmitInvoiceCommand, InvoiceDto>
{
    public async Task<InvoiceDto> Handle(EmitInvoiceCommand request, CancellationToken ct)
    {
        var order = await db.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Ready)
            throw new DomainException(
                $"Solo se factura un pedido entregado/listo. Estado actual: '{order.Status}'.");

        var existing = await db.Invoices.AnyAsync(i => i.OrderId == order.Id, ct)
            .ConfigureAwait(false);
        if (existing)
            throw new DomainException("Esta orden ya tiene factura emitida.");

        // Determine if IGTF applies (verified payment in foreign currency).
        var verifiedMethod = await db.Payments.AsNoTracking()
            .Where(p => p.OrderId == order.Id && p.Status == PaymentStatus.Verified)
            .OrderByDescending(p => p.VerifiedAt)
            .Select(p => (PaymentMethod?)p.Method)
            .FirstOrDefaultAsync(ct).ConfigureAwait(false);

        var igtfApplies = verifiedMethod is { } m
            && settings.IgtfPaymentMethods.Contains(EnumDbMap<PaymentMethod>.ToDb(m));

        // Pasada C / H-03: el Issuer (RIF, razon social, direccion fiscal) viene
        // del Chef de la order. Antes era global de appsettings.json.
        var chef = await db.Chefs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == order.ChefId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(HomeChefPro.Domain.Tenancy.Chef), order.ChefId);

        var invoice = Invoice.CreateDraft(
            orderId: order.Id,
            subtotalUsd: order.SubtotalUsd,
            ivaRate: settings.IvaRate,
            igtfRate: settings.IgtfRate,
            igtfApplies: igtfApplies,
            provider: provider.ProviderName,
            issuerRif: chef.Rif,
            issuerLegalName: chef.LegalName,
            issuerAddress: chef.TaxAddress,
            customerRif: request.CustomerRif,
            customerLegalName: request.CustomerLegalName,
            customerAddress: request.CustomerAddress,
            clock: clock);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Call the provider. If it fails, mark the invoice as failed and re-save.
        try
        {
            var emission = await provider.EmitAsync(new FiscalEmissionRequest(
                InvoiceId: invoice.Id,
                OrderId: order.Id,
                OrderNumber: order.OrderNumber,
                SubtotalUsd: invoice.SubtotalUsd,
                IvaUsd: invoice.IvaUsd,
                IgtfUsd: invoice.IgtfUsd,
                TotalWithTaxUsd: invoice.TotalWithTaxUsd,
                IssuerRif: invoice.IssuerRif,
                IssuerLegalName: invoice.IssuerLegalName,
                CustomerRif: invoice.CustomerRif,
                CustomerLegalName: invoice.CustomerLegalName,
                Lines: order.Items.Select(i => new FiscalLine(
                    DishName: i.DishNameSnapshot,
                    Quantity: i.Quantity,
                    UnitPriceUsd: i.UnitPriceUsd,
                    LineTotalUsd: i.LineTotalUsd)).ToList()),
                ct).ConfigureAwait(false);

            if (emission.Succeeded
                && emission.FiscalNumber is not null
                && emission.ControlNumber is not null)
            {
                invoice.MarkIssued(
                    emission.FiscalNumber,
                    emission.ControlNumber,
                    currentUser.RequireUserId(),
                    emission.RawResponseJson,
                    clock);
            }
            else
            {
                invoice.MarkFailed(
                    emission.RawResponseJson ?? emission.FailureReason ?? "Provider failure",
                    clock);
            }
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            invoice.MarkFailed($"{{\"error\":\"{ex.Message}\"}}", clock);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return invoice.ToDto(order.OrderNumber);
    }
}

/// <summary>Pulled from configuration (Tax section only). Pasada C / H-03:
/// el Issuer ya no vive aca — lo lee el handler del Chef de la orden.</summary>
public sealed record InvoicingSettings(
    decimal IvaRate,
    decimal IgtfRate,
    IReadOnlyList<string> IgtfPaymentMethods);
