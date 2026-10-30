namespace WebShop.Infrastructure;

/// <summary>Bound to the "Shop" section of appsettings.json.</summary>
public sealed class ShopSettings
{
    public const string SectionName = "Shop";

    /// <summary>Shop name, shown as the wordmark and in page titles.</summary>
    public string Name { get; set; } = "Jumbus";

    public string Currency { get; set; } = "RSD";

    /// <summary>Serbian VAT, stored as a fraction (0.20 = 20%).</summary>
    public decimal VatRate { get; set; } = 0.20m;

    public decimal FreeShippingThreshold { get; set; } = 6000m;

    public decimal StandardShippingFee { get; set; } = 390m;

    /// <summary>
    /// Withdrawal window shown in the hero. Serbian distance-selling law sets
    /// a minimum of 14 days; raising it is allowed, lowering it is not.
    /// </summary>
    public int ReturnDays { get; set; } = 14;

    public string AdminEmail { get; set; } = "admin@webshop.rs";

    public string AdminPassword { get; set; } = "Admin123!";

    public bool SeedOnStartup { get; set; } = true;

    /// <summary>
    /// The shop's public address, e.g. https://www.jumbus.rs, used for
    /// canonical links, hreflang, the sitemap and social previews. Left empty,
    /// the host of the current request is used: right on localhost, wrong
    /// behind a proxy that rewrites the host or terminates HTTPS.
    /// </summary>
    public string? BaseUrl { get; set; }

    /// <summary>The seller, as the law requires it to be shown on the site.</summary>
    public CompanyInfo Company { get; set; } = new();
}

/// <summary>
/// The legal identity of the seller. Serbian consumer and e-commerce law
/// require a distance seller to publish these; the footer, the legal pages,
/// the withdrawal form and the structured data all read them from here.
/// </summary>
public sealed class CompanyInfo
{
    public string LegalName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    /// <summary>PIB.</summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>Matični broj, from the business register (APR).</summary>
    public string RegistrationNumber { get; set; } = string.Empty;

    /// <summary>Šifra delatnosti; 4791 is retail sale via mail order or the internet.</summary>
    public string ActivityCode { get; set; } = "4791";

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    /// <summary>When the phone is answered, e.g. "09–17", shown as "weekdays 09–17".</summary>
    public string SupportHours { get; set; } = string.Empty;

    /// <summary>Where returned goods go. Empty means the registered address.</summary>
    public string? ReturnsAddress { get; set; }

    public string Address => $"{Street}, {PostalCode} {City}".Trim(' ', ',');

    public string ReturnsTo => string.IsNullOrWhiteSpace(ReturnsAddress) ? Address : ReturnsAddress;
}
