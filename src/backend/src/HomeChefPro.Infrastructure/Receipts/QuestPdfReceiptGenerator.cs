using System.Globalization;
using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Application.Orders.Dtos;
using HomeChefPro.Application.Payments.Dtos;
using HomeChefPro.Application.Receipts.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HomeChefPro.Infrastructure.Receipts;

public sealed class QuestPdfReceiptGenerator : IReceiptPdfGenerator
{
    private static readonly CultureInfo UsdCulture = CultureInfo.GetCultureInfo("en-US");
    private static readonly CultureInfo VesCulture = CultureInfo.GetCultureInfo("es-VE");

    static QuestPdfReceiptGenerator()
    {
        // Set once per process. Community license is permitted for this project's scale.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(
        OrderDto order,
        PaymentDto? verifiedPayment,
        string? customerName,
        InvoiceDto? invoice = null)
    {
        var isFiscalDoc = invoice is { Status: "issued" or "cancelled" };
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(24);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(t => t.FontSize(10).FontFamily("Helvetica"));

                page.Header().Element(c => BuildHeader(c, order, invoice, isFiscalDoc));
                page.Content().Element(c => BuildContent(c, order, verifiedPayment, customerName, invoice));
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span(isFiscalDoc
                            ? "Documento fiscal · HomeChef Pro"
                            : "HomeChef Pro · Cocina casera venezolana")
                        .FontSize(8).FontColor(Colors.Grey.Darken1);
                });

                if (invoice?.Status == "cancelled")
                {
                    page.Foreground().AlignMiddle().AlignCenter().Text("ANULADA")
                        .FontSize(72).Bold().FontColor(Colors.Red.Lighten3);
                }
            });
        });

        return document.GeneratePdf();
    }

    private static void BuildHeader(
        QuestPDF.Infrastructure.IContainer container,
        OrderDto order,
        InvoiceDto? invoice,
        bool isFiscalDoc)
    {
        container.PaddingBottom(10).BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(invoice?.IssuerLegalName ?? "HomeChef Pro").FontSize(18).Bold();
                    c.Item().Text(isFiscalDoc ? "Factura" : "Recibo de pedido")
                        .FontSize(11).FontColor(Colors.Grey.Darken2);
                });
                if (isFiscalDoc && invoice is not null)
                {
                    row.ConstantItem(160).AlignRight().Column(c =>
                    {
                        c.Item().Text(invoice.FiscalNumber ?? "")
                            .FontSize(13).Bold().FontColor(Colors.Red.Darken1);
                        if (!string.IsNullOrEmpty(invoice.ControlNumber))
                            c.Item().Text($"Control: {invoice.ControlNumber}").FontSize(9);
                        if (!string.IsNullOrEmpty(invoice.IssuerRif))
                            c.Item().Text($"RIF: {invoice.IssuerRif}").FontSize(9);
                    });
                }
            });
            col.Item().PaddingTop(4).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Pedido: ").SemiBold();
                    t.Span(string.IsNullOrEmpty(order.OrderNumber) ? "(pendiente)" : order.OrderNumber);
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Fecha: ").SemiBold();
                    var date = invoice?.IssuedAt ?? order.CreatedAt;
                    t.Span(date.ToLocalTime().ToString("dd MMM yyyy HH:mm", VesCulture));
                });
            });
        });
    }

    private static void BuildContent(
        QuestPDF.Infrastructure.IContainer container,
        OrderDto order,
        PaymentDto? payment,
        string? customerName,
        InvoiceDto? invoice)
    {
        container.PaddingVertical(10).Column(col =>
        {
            col.Spacing(10);

            // Issuer + customer block on fiscal documents (RIF + razón social).
            if (invoice is { Status: "issued" or "cancelled" })
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Column(c =>
                        {
                            c.Item().Text("Emisor").SemiBold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(invoice.IssuerLegalName ?? "—");
                            if (!string.IsNullOrEmpty(invoice.IssuerRif))
                                c.Item().Text($"RIF: {invoice.IssuerRif}").FontSize(9);
                        });
                    row.ConstantItem(8);
                    row.RelativeItem().Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .Padding(8).Column(c =>
                        {
                            c.Item().Text("Cliente").SemiBold().FontSize(9).FontColor(Colors.Grey.Darken1);
                            c.Item().Text(invoice.CustomerLegalName ?? customerName ?? "Consumidor final");
                            if (!string.IsNullOrEmpty(invoice.CustomerRif))
                                c.Item().Text($"RIF: {invoice.CustomerRif}").FontSize(9);
                        });
                });
            }

            col.Item().Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span("Cliente: ").SemiBold();
                    t.Span(customerName ?? "Invitado");
                });
                row.RelativeItem().AlignRight().Text(t =>
                {
                    t.Span("Entrega: ").SemiBold();
                    t.Span(order.DeliveryType == "pickup" ? "Retiro en local" : "Delivery");
                });
            });

            if (order.DeliveryType == "third_party" && !string.IsNullOrWhiteSpace(order.DeliveryAddress))
            {
                col.Item().Text(t =>
                {
                    t.Span("Dirección: ").SemiBold();
                    t.Span(order.DeliveryAddress);
                });
            }

            if (!string.IsNullOrWhiteSpace(order.CustomerNotes))
            {
                col.Item().Text(t =>
                {
                    t.Span("Notas: ").SemiBold();
                    t.Span(order.CustomerNotes);
                });
            }

            // Items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.RelativeColumn(5);  // dish
                    cols.RelativeColumn(1);  // qty
                    cols.RelativeColumn(2);  // unit price
                    cols.RelativeColumn(2);  // line total
                });

                table.Header(header =>
                {
                    header.Cell().Text("Plato").SemiBold();
                    header.Cell().AlignRight().Text("Cant.").SemiBold();
                    header.Cell().AlignRight().Text("P/U").SemiBold();
                    header.Cell().AlignRight().Text("Total").SemiBold();
                });

                foreach (var item in order.Items)
                {
                    table.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).Column(c =>
                        {
                            c.Item().Text(item.DishNameSnapshot);
                            if (!string.IsNullOrWhiteSpace(item.ItemNotes))
                                c.Item().Text(item.ItemNotes).FontSize(8).FontColor(Colors.Grey.Darken1);
                        });
                    table.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text(item.Quantity.ToString(UsdCulture));
                    table.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text(FormatUsd(item.UnitPriceUsd));
                    table.Cell().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2)
                        .PaddingVertical(4).AlignRight().Text(FormatUsd(item.LineTotalUsd));
                }
            });

            // Totals
            col.Item().PaddingTop(6).AlignRight().Column(tot =>
            {
                tot.Item().Text($"Subtotal: {FormatUsd(order.SubtotalUsd)}");
                if (order.DiscountUsd > 0)
                    tot.Item().Text($"Descuento: -{FormatUsd(order.DiscountUsd)}");
                if (order.DeliveryFeeUsd > 0)
                    tot.Item().Text($"Envío: {FormatUsd(order.DeliveryFeeUsd)}");

                if (invoice is { Status: "issued" or "cancelled" })
                {
                    tot.Item().Text($"IVA (16%): {FormatUsd(invoice.IvaUsd)}");
                    if (invoice.IgtfApplies)
                        tot.Item().Text($"IGTF (3%): {FormatUsd(invoice.IgtfUsd)}");
                    tot.Item().PaddingTop(4)
                        .Text($"Total: {FormatUsd(invoice.TotalWithTaxUsd)}")
                        .FontSize(13).Bold();
                }
                else
                {
                    tot.Item().PaddingTop(4)
                        .Text($"Total: {FormatUsd(order.TotalUsd)}")
                        .FontSize(13).Bold();
                }

                if (order.TotalVesAtOrderTime is { } ves && order.RateVesPerUsdAtOrder is { } rate)
                {
                    tot.Item().Text($"Equivalente: {FormatVes(ves)} (tasa {FormatVes(rate)}/USD)")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                }
            });

            // Payment
            if (payment is not null)
            {
                col.Item().PaddingTop(10).Border(0.5f).BorderColor(Colors.Grey.Lighten2)
                    .Padding(8).Column(pay =>
                    {
                        pay.Item().Text("Pago verificado").SemiBold();
                        pay.Item().Text($"Método: {HumanizeMethod(payment.Method)}");
                        pay.Item().Text($"Moneda: {payment.PaidCurrency}  ·  Monto: " +
                            (payment.PaidCurrency == "VES"
                                ? FormatVes(payment.AmountPaidCurrency)
                                : FormatUsd(payment.AmountPaidCurrency)));
                        if (!string.IsNullOrWhiteSpace(payment.ReferenceNumber))
                            pay.Item().Text($"Referencia: {payment.ReferenceNumber}");
                        if (payment.VerifiedAt is { } vt)
                            pay.Item().Text(
                                $"Verificado: {vt.ToLocalTime().ToString("dd MMM yyyy HH:mm", VesCulture)}")
                                .FontSize(9).FontColor(Colors.Grey.Darken1);
                    });
            }
            else
            {
                col.Item().PaddingTop(10).Text("Pago pendiente de verificación.")
                    .FontColor(Colors.Red.Medium).Italic();
            }
        });
    }

    private static string FormatUsd(decimal amount) =>
        "$" + amount.ToString("N2", UsdCulture);

    private static string FormatVes(decimal amount) =>
        "Bs " + amount.ToString("N2", VesCulture);

    private static string HumanizeMethod(string method) => method switch
    {
        "pago_movil"   => "Pago Móvil",
        "transfer_ves" => "Transferencia VES",
        "transfer_usd" => "Transferencia USD",
        "zelle"        => "Zelle",
        "binance_pay"  => "Binance Pay",
        "cash"         => "Efectivo",
        _              => method,
    };
}
