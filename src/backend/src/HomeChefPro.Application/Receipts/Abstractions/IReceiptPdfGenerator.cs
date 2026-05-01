using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Payments.Dtos;

namespace HomeChefPro.Application.Receipts.Abstractions;

public interface IReceiptPdfGenerator
{
    /// <summary>
    /// Renders the order receipt as a PDF. When <paramref name="invoice"/> is non-null
    /// and its status is 'issued' or 'cancelled', the document switches to a tax-invoice
    /// layout (RIFs, fiscal/control numbers, IVA + IGTF breakdown). 'cancelled' shows an
    /// ANULADA watermark.
    /// </summary>
    byte[] Render(
        OrderDto order,
        PaymentDto? verifiedPayment,
        string? customerName,
        InvoiceDto? invoice = null);
}
