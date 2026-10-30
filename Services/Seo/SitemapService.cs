using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Services.Catalog;

namespace WebShop.Services.Seo;

/// <summary>One page of the shop, with its address in every language it exists in.</summary>
public sealed record SitemapPage(IReadOnlyDictionary<string, string> PathsByCulture);

public interface ISitemapService
{
    Task<IReadOnlyList<SitemapPage>> PagesAsync(CancellationToken ct);
}

/// <summary>
/// Everything a search engine should index: the home page, the catalogue,
/// active categories, published products and the information pages. Cart,
/// checkout and account pages are left out on purpose.
///
/// No lastmod: the shop does not record when a product was last edited, and
/// a made-up date is worse than none — search engines learn to ignore it.
/// </summary>
public class SitemapService : ISitemapService
{
    public static readonly string[] InformationPages =
        ["Shipping", "Returns", "WithdrawalForm", "SizeGuide", "Contact", "Terms", "Privacy", "Cookies"];

    private readonly ApplicationDbContext _context;
    private readonly LinkGenerator _links;

    public SitemapService(ApplicationDbContext context, LinkGenerator links)
    {
        _context = context;
        _links = links;
    }

    public async Task<IReadOnlyList<SitemapPage>> PagesAsync(CancellationToken ct)
    {
        var pages = new List<SitemapPage>
        {
            Fixed("Home", "Index"),
            Fixed("Products", "Index")
        };

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive && (c.ParentId == null || c.Parent!.IsActive))
            .OrderBy(c => c.ParentId)
            .ThenBy(c => c.DisplayOrder)
            .Select(c => new
            {
                c.Id,
                Slugs = c.Translations
                    .Select(t => new CatalogService.SlugPair { Culture = t.Culture, Slug = t.Slug })
                    .ToList()
            })
            .ToListAsync(ct);

        foreach (var category in categories)
        {
            var paths = new Dictionary<string, string>();

            foreach (var slug in category.Slugs.Where(s => CultureConstants.IsSupported(s.Culture)))
            {
                if (_links.GetPathByAction("Category", "Products",
                        new { area = "", culture = slug.Culture, categorySlug = slug.Slug }) is { } path)
                {
                    paths[slug.Culture] = path;
                }
            }

            Add(pages, paths);
        }

        var products = await _context.Products
            .AsNoTracking()
            .AsSplitQuery()
            .Where(p => p.Status == ProductStatus.Published && p.Category.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => new
            {
                Slugs = p.Translations
                    .Select(t => new CatalogService.SlugPair { Culture = t.Culture, Slug = t.Slug })
                    .ToList(),
                CategorySlugs = p.Category.Translations
                    .Select(t => new CatalogService.SlugPair { Culture = t.Culture, Slug = t.Slug })
                    .ToList()
            })
            .ToListAsync(ct);

        foreach (var product in products)
        {
            var paths = new Dictionary<string, string>();

            foreach (var slug in product.Slugs.Where(s => CultureConstants.IsSupported(s.Culture)))
            {
                var categorySlug = product.CategorySlugs.FirstOrDefault(c => c.Culture == slug.Culture)?.Slug;

                if (categorySlug is not null
                    && _links.GetPathByAction("Details", "Products",
                        new { area = "", culture = slug.Culture, categorySlug, productSlug = slug.Slug }) is { } path)
                {
                    paths[slug.Culture] = path;
                }
            }

            Add(pages, paths);
        }

        pages.AddRange(InformationPages.Select(action => Fixed("Legal", action)));

        return pages;
    }

    private SitemapPage Fixed(string controller, string action)
    {
        var paths = new Dictionary<string, string>();

        foreach (var code in CultureConstants.SupportedRouteCodes)
        {
            if (_links.GetPathByAction(action, controller, new { area = "", culture = code }) is { } path)
            {
                paths[code] = path;
            }
        }

        return new SitemapPage(paths);
    }

    private static void Add(List<SitemapPage> pages, Dictionary<string, string> paths)
    {
        if (paths.Count > 0)
        {
            pages.Add(new SitemapPage(paths));
        }
    }
}
