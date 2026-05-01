using HomeChefPro.Application.Invoicing.Dtos;
using HomeChefPro.Domain.Common;
using HomeChefPro.Domain.Invoicing;

namespace HomeChefPro.Application.Invoicing.Mapping;

public static class InvoiceMapping
{
    public static InvoiceDto ToDto(this Invoice i, string orderNumber) =>
        new(
            Id: i.Id,
            OrderId: i.OrderId,
            OrderNumber: orderNumber,
            Status: EnumDbMap<InvoiceStatus>.ToDb(i.Status),
            SubtotalUsd: i.SubtotalUsd,
            IvaUsd: i.IvaUsd,
            IgtfUsd: i.IgtfUsd,
            TotalWithTaxUsd: i.TotalWithTaxUsd,
            IgtfApplies: i.IgtfApplies,
            Provider: i.Provider,
            FiscalNumber: i.FiscalNumber,
            ControlNumber: i.ControlNumber,
            IssuerRif: i.IssuerRif,
            IssuerLegalName: i.IssuerLegalName,
            CustomerRif: i.CustomerRif,
            CustomerLegalName: i.CustomerLegalName,
            CreatedAt: i.CreatedAt,
            IssuedAt: i.IssuedAt,
            CancelledAt: i.CancelledAt,
            CancellationReason: i.CancellationReason);
}
