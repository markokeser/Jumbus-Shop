using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Html;
using WebShop.ViewModels.Catalog;

namespace WebShop.Infrastructure;

/// <summary>
/// schema.org JSON-LD for search results: products with price and stock,
/// breadcrumbs, and the shop itself.
///
/// Built as data and serialised, rather than written as JSON inside Razor,
/// so a product name with a quote or a "&lt;/script&gt;" in it can never break
/// out of the script block. The encoder keeps č, ć, ž readable but still
/// escapes &lt;, &gt; and &amp;.
/// </summary>
public static class StructuredData
{
    private const string Context = "https://schema.org";

    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static IHtmlContent Script(params object[] graphs)
    {
        var payload = graphs.Length == 1 ? graphs[0] : graphs;
        return new HtmlString(
            "<script type=\"application/ld+json\">" + JsonSerializer.Serialize(payload, Options) + "</script>");
    }

    public static Dictionary<string, object?> Breadcrumbs(IEnumerable<(string Name, string Url)> items) => new()
    {
        ["@context"] = Context,
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = items
            .Select((item, index) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = index + 1,
                ["name"] = item.Name,
                ["item"] = item.Url
            })
            .ToList()
    };

    /// <summary>
    /// One Offer per variant rather than an AggregateOffer: Google's merchant
    /// listings read individual offers only, and each variant has its own
    /// price and stock anyway.
    /// </summary>
    public static Dictionary<string, object?> Product(
        ProductDetailsViewModel product,
        string url,
        IEnumerable<string> imageUrls,
        string currency)
    {
        var images = imageUrls.ToList();

        var data = new Dictionary<string, object?>
        {
            ["@context"] = Context,
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["sku"] = product.Sku,
            ["url"] = url,
            ["description"] = product.MetaDescription ?? product.ShortDescription ?? product.Description,
            ["image"] = images.Count > 0 ? images : null,
            ["brand"] = string.IsNullOrEmpty(product.BrandName)
                ? null
                : new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = product.BrandName },
            ["offers"] = product.Variants.Count == 0
                ? null
                : product.Variants.Select(v => new Dictionary<string, object?>
                {
                    ["@type"] = "Offer",
                    ["sku"] = v.Sku,
                    ["price"] = v.EffectivePrice.ToString("0.00", CultureInfo.InvariantCulture),
                    ["priceCurrency"] = currency,
                    ["availability"] = v.StockQuantity > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
                    ["itemCondition"] = "https://schema.org/NewCondition",
                    ["url"] = url
                }).ToList()
        };

        if (product.Reviews.Count > 0)
        {
            data["aggregateRating"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateRating",
                ["ratingValue"] = Math.Round(product.Reviews.Average, 1).ToString("0.0", CultureInfo.InvariantCulture),
                ["reviewCount"] = product.Reviews.Count,
                ["bestRating"] = 5,
                ["worstRating"] = 1
            };
        }

        return data;
    }

    public static Dictionary<string, object?> Organization(ShopSettings shop, string homeUrl) => new()
    {
        ["@context"] = Context,
        ["@type"] = "OnlineStore",
        ["name"] = shop.Name,
        ["legalName"] = NullIfEmpty(shop.Company.LegalName),
        ["url"] = homeUrl,
        ["email"] = NullIfEmpty(shop.Company.Email),
        ["telephone"] = NullIfEmpty(shop.Company.Phone),
        ["taxID"] = NullIfEmpty(shop.Company.TaxId),
        ["address"] = string.IsNullOrWhiteSpace(shop.Company.Street)
            ? null
            : new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = shop.Company.Street,
                ["postalCode"] = NullIfEmpty(shop.Company.PostalCode),
                ["addressLocality"] = NullIfEmpty(shop.Company.City),
                ["addressCountry"] = "RS"
            }
    };

    public static Dictionary<string, object?> WebSite(ShopSettings shop, string homeUrl, string language) => new()
    {
        ["@context"] = Context,
        ["@type"] = "WebSite",
        ["name"] = shop.Name,
        ["url"] = homeUrl,
        ["inLanguage"] = language
    };

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
