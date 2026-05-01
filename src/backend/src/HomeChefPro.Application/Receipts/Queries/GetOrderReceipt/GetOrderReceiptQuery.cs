using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using HomeChefPro.Application.Invoicing.Mapping;
using HomeChefPro.Application.Orders.Mapping;
using HomeChefPro.Application.Payments.Mapping;
using HomeChefPro.Application.Receipts.Abstractions;
using HomeChefPro.Application.Receipts.Dtos;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Orders;
using HomeChefPro.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HomeChefPro.Application.Receipts.Queries.GetOrderReceipt;

public sealed record GetOrderReceiptQuery(Guid OrderId) : IRequest<ReceiptPdfDto>;

public sealed class GetOrderReceiptHandler(
    IHomeChefProDbContext db,
    IReceiptPdfGenerator pdfGenerator)
    : IRequestHandler<GetOrderReceiptQuery, ReceiptPdfDto>
{
    public async Task<ReceiptPdfDto> Handle(GetOrderReceiptQuery request, CancellationToken ct)
    {
        var order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(Order), request.OrderId);

        var verifiedPayment = await db.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == order.Id && p.Status == PaymentStatus.Verified)
            .OrderByDescending(p => p.VerifiedAt)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        // Customer name — either the registered profile or the guest.
        string? customerName = null;
        if (order.UserId is { } uid)
        {
            customerName = await db.UserProfiles.AsNoTracking()
                .Where(p => p.Id == uid).Select(p => p.FullName).FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }
        else if (order.GuestCustomerId is { } gid)
        {
            customerName = await db.GuestCustomers.AsNoTracking()
                .Where(g => g.Id == gid).Select(g => g.FullName).FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);
        }

        // If a fiscal invoice exists for this order, fold it into the PDF so the
        // document doubles as the official receipt with tax breakdown.
        var invoice = await db.Invoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.OrderId == order.Id, ct)
            .ConfigureAwait(false);

        var orderDto = order.ToDto();
        var paymentDto = verifiedPayment?.ToDto();
        var invoiceDto = invoice?.ToDto(order.OrderNumber);
        var pdf = pdfGenerator.Render(orderDto, paymentDto, customerName, invoiceDto);

        var prefix = invoiceDto?.Status == "issued" ? "factura" : "recibo";
        var number = !string.IsNullOrEmpty(invoiceDto?.FiscalNumber)
            ? invoiceDto!.FiscalNumber
            : (string.IsNullOrEmpty(order.OrderNumber) ? order.Id.ToString("N") : order.OrderNumber);
        return new ReceiptPdfDto(pdf, $"{prefix}-{number}.pdf");
    }
}
