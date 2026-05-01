namespace HomeChefPro.Infrastructure.Invoicing;

public sealed class TaxOptions
{
    public const string SectionName = "Tax";

    /// <summary>IVA Venezuela (al 2026) = 0.16 (16%).</summary>
    public decimal IvaRate { get; set; } = 0.16m;

    /// <summary>IGTF Venezuela = 0.03 (3%) sobre pagos en divisas.</summary>
    public decimal IgtfRate { get; set; } = 0.03m;

    /// <summary>Métodos de pago que disparan IGTF por considerarse divisas.</summary>
    public string[] IgtfPaymentMethods { get; set; } =
        new[] { "transfer_usd", "zelle", "binance_pay" };
}

public sealed class IssuerOptions
{
    public const string SectionName = "Issuer";

    public string? Rif { get; set; }
    public string? LegalName { get; set; }
    public string? Address { get; set; }
}
