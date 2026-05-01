using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Invoicing;

public enum InvoiceStatus
{
    [DbValue("draft")]     Draft,
    [DbValue("issued")]    Issued,
    [DbValue("cancelled")] Cancelled,
    [DbValue("failed")]    Failed,
}
