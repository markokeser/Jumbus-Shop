using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebShop.Infrastructure;
using WebShop.Services.Seo;

namespace WebShop.Controllers;

/// <summary>
/// /sitemap.xml and /robots.txt. Both live outside the language prefix —
/// crawlers look for them at the root — and CulturePrefixRedirectMiddleware
/// leaves them alone for that reason.
/// </summary>
public class SeoController : Controller
{
    /// <summary>Pages that are personal or transactional: never worth crawling.</summary>
    private static readonly string[] PrivateSections =
        ["cart", "checkout", "account", "orders", "addresses", "wishlist"];

    private readonly ISitemapService _sitemap;
    private readonly ShopSettings _shop;

    public SeoController(ISitemapService sitemap, IOptions<ShopSettings> shop)
    {
        _sitemap = sitemap;
        _shop = shop.Value;
    }

    /// <summary>
    /// One &lt;url&gt; per language, each listing all its language versions,
    /// which is the form search engines expect for translated pages.
    /// </summary>
    [HttpGet("sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    public async Task<IActionResult> Sitemap(CancellationToken ct)
    {
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";

        var origin = SiteUrls.Origin(HttpContext, _shop);
        var urlset = new XElement(ns + "urlset", new XAttribute(XNamespace.Xmlns + "xhtml", xhtml));

        foreach (var page in await _sitemap.PagesAsync(ct))
        {
            foreach (var path in page.PathsByCulture.Values)
            {
                var url = new XElement(ns + "url", new XElement(ns + "loc", origin + path));

                if (page.PathsByCulture.Count > 1)
                {
                    foreach (var (culture, alternate) in page.PathsByCulture)
                    {
                        url.Add(Link(xhtml, culture, origin + alternate));
                    }

                    if (page.PathsByCulture.TryGetValue(CultureConstants.Default, out var fallback))
                    {
                        url.Add(Link(xhtml, "x-default", origin + fallback));
                    }
                }

                urlset.Add(url);
            }
        }

        var xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + urlset.ToString(SaveOptions.DisableFormatting);
        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("robots.txt")]
    [ResponseCache(Duration = 3600)]
    public IActionResult Robots()
    {
        var lines = new List<string> { "User-agent: *", "Disallow: /admin", "Disallow: /status/" };

        foreach (var code in CultureConstants.SupportedRouteCodes)
        {
            lines.AddRange(PrivateSections.Select(section => $"Disallow: /{code}/{section}"));
        }

        lines.Add(string.Empty);
        lines.Add($"Sitemap: {SiteUrls.Origin(HttpContext, _shop)}/sitemap.xml");

        return Content(string.Join('\n', lines) + "\n", "text/plain; charset=utf-8");
    }

    private static XElement Link(XNamespace xhtml, string hreflang, string href) =>
        new(xhtml + "link",
            new XAttribute("rel", "alternate"),
            new XAttribute("hreflang", hreflang),
            new XAttribute("href", href));
}
