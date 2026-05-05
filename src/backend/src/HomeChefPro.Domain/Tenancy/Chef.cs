using HomeChefPro.Domain.Common;

namespace HomeChefPro.Domain.Tenancy;

/// <summary>
/// Multi-tenant root del SaaS HomeChef Pro. Cada Chef es un inquilino con su
/// propia identidad fiscal (RIF + razon social SENIAT), zona horaria, prefix
/// de correlativo de orders y configuracion operacional.
///
/// Pasada C / Fase 1C-A (Bloque 2): se introduce la entity para que el codigo
/// .NET pueda referenciar al chef. En esta fase, todas las entities tienen
/// `ChefId` con default al UUID del piloto via SQL DEFAULT — Fase 2 quitara
/// el default y cada Insert exigira un ChefId explicito.
/// </summary>
public sealed class Chef : AggregateRoot<Guid>
{
    public string Rif { get; private set; } = null!;
    public string LegalName { get; private set; } = null!;
    public string? TradeName { get; private set; }
    public string TaxAddress { get; private set; } = null!;

    public string Timezone { get; private set; } = "America/Caracas";
    public string BaseCurrency { get; private set; } = "USD";
    public string DisplayCurrency { get; private set; } = "VES";
    public string InvoicePrefix { get; private set; } = "HC";

    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }

    public ChefStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? SuspendedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    /// <summary>
    /// UUID determinista del chef piloto. Coincide con el seed en
    /// <c>01b_chefs.sql</c> y con el SQL DEFAULT de las columnas
    /// <c>chef_id</c> de todas las tablas de negocio.
    /// </summary>
    public static readonly Guid PilotoId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private Chef() { }

    private Chef(
        Guid id,
        string rif,
        string legalName,
        string? tradeName,
        string taxAddress,
        string timezone,
        string invoicePrefix,
        string? contactEmail,
        string? contactPhone,
        DateTimeOffset now)
    {
        Id = id;
        Rif = rif;
        LegalName = legalName;
        TradeName = tradeName;
        TaxAddress = taxAddress;
        Timezone = timezone;
        BaseCurrency = "USD";
        DisplayCurrency = "VES";
        InvoicePrefix = invoicePrefix;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        Status = ChefStatus.Active;
        CreatedAt = now;
        ActivatedAt = now;
    }

    public static Chef Create(
        Guid id,
        string rif,
        string legalName,
        string taxAddress,
        DateTimeOffset now,
        string? tradeName = null,
        string timezone = "America/Caracas",
        string invoicePrefix = "HC",
        string? contactEmail = null,
        string? contactPhone = null)
    {
        if (string.IsNullOrWhiteSpace(rif))
            throw new DomainException("Chef RIF is required.");
        if (string.IsNullOrWhiteSpace(legalName))
            throw new DomainException("Chef legal name is required.");
        if (string.IsNullOrWhiteSpace(taxAddress))
            throw new DomainException("Chef tax address is required.");
        if (string.IsNullOrWhiteSpace(invoicePrefix) || invoicePrefix.Length > 4)
            throw new DomainException("Chef invoice prefix is required and must be at most 4 chars.");

        return new Chef(
            id: id,
            rif: rif,
            legalName: legalName,
            tradeName: tradeName,
            taxAddress: taxAddress,
            timezone: timezone,
            invoicePrefix: invoicePrefix,
            contactEmail: contactEmail,
            contactPhone: contactPhone,
            now: now);
    }

    public void Suspend(DateTimeOffset now)
    {
        if (Status == ChefStatus.Archived)
            throw new DomainException("Cannot suspend an archived chef.");
        Status = ChefStatus.Suspended;
        SuspendedAt = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        if (Status == ChefStatus.Archived)
            throw new DomainException("Cannot reactivate an archived chef.");
        Status = ChefStatus.Active;
        ActivatedAt = now;
        SuspendedAt = null;
    }

    public void Archive(DateTimeOffset now)
    {
        Status = ChefStatus.Archived;
        ArchivedAt = now;
    }

    public void UpdateFiscalProfile(string rif, string legalName, string taxAddress, string? tradeName)
    {
        if (string.IsNullOrWhiteSpace(rif)) throw new DomainException("RIF required.");
        if (string.IsNullOrWhiteSpace(legalName)) throw new DomainException("Legal name required.");
        if (string.IsNullOrWhiteSpace(taxAddress)) throw new DomainException("Tax address required.");

        Rif = rif;
        LegalName = legalName;
        TaxAddress = taxAddress;
        TradeName = tradeName;
    }
}
