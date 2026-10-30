using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Services.Catalog;
using WebShop.Services.Customers;
using WebShop.Services.Reviews;

namespace WebShop.Controllers;

public class ProductsController : Controller
{
    /// <summary>
    /// Query keys the page owns. Anything else is a candidate attribute
    /// filter and gets checked against the attribute table.
    /// </summary>
    private static readonly HashSet<string> ReservedQueryKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "q", "brand", "min", "max", "stock", "sale", "sort", "page"
    };

    /// <summary>Query keys that are really route values and must never be carried over.</summary>
    private static readonly HashSet<string> RouteValueKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "culture", "categorySlug", "productSlug", "controller", "action", "area"
    };

    private readonly ICatalogService _catalog;
    private readonly IReviewService _reviews;
    private readonly IWishlistService _wishlist;

    public ProductsController(
        ICatalogService catalog,
        IReviewService reviews,
        IWishlistService wishlist)
    {
        _catalog = catalog;
        _reviews = reviews;
        _wishlist = wishlist;
    }

    /// <summary>Everything, plus search. Reached at /sr/products.</summary>
    public async Task<IActionResult> Index([FromQuery] ProductQuery query, CancellationToken ct)
    {
        var culture = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

        await CollectAttributeFiltersAsync(query, ct);

        return View(await _catalog.ListAsync(query, culture, ct));
    }

    /// <summary>
    /// One category and everything beneath it. Reached at /sr/haljine, which
    /// is why the action takes a slug rather than an id: the URL is a product
    /// of translation, not of the database key.
    /// </summary>
    public async Task<IActionResult> Category(
        string categorySlug,
        [FromQuery] ProductQuery query,
        CancellationToken ct)
    {
        var culture = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

        var category = await _catalog.FindCategoryAsync(categorySlug, culture, ct);

        if (category is null)
        {
            // Most likely the slug from the other language, which is what a
            // pasted or bookmarked link looks like after a language switch.
            return await RescueCategoryAsync(categorySlug, culture, ct);
        }

        await CollectAttributeFiltersAsync(query, ct);
        query.CategoryId = category.Id;

        var model = await _catalog.ListAsync(query, culture, ct);
        model.Category = category;

        SetAlternates(category.SlugsByCulture, productSlugs: null);

        return View(nameof(Index), model);
    }

    /// <summary>Reached at /sr/haljine/koktel-haljina-vera.</summary>
    public async Task<IActionResult> Details(
        string categorySlug,
        string productSlug,
        CancellationToken ct)
    {
        var culture = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);

        var product = await _catalog.FindProductAsync(productSlug, culture, ct);

        if (product is null)
        {
            return await RescueProductAsync(productSlug, culture, ct);
        }

        // A product reached under the wrong category — an old link, or one
        // typed by hand — still resolves, but only after being sent to its
        // real address, so one page never sits at two URLs.
        if (!string.Equals(product.CategorySlug, categorySlug, StringComparison.OrdinalIgnoreCase))
        {
            return RedirectToActionPermanent(nameof(Details), new
            {
                categorySlug = product.CategorySlug,
                productSlug = product.Slug
            });
        }

        SetAlternates(product.CategorySlugsByCulture, product.SlugsByCulture);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        product.Reviews = await _reviews.SummaryAsync(product.Id, userId, ct);
        product.IsInWishlist = userId is not null
            && await _wishlist.ContainsAsync(userId, product.Id, ct);

        return View(product);
    }

    /// <summary>
    /// Records where this page lives in the other languages, so the switcher
    /// in the layout points at the same category or product instead of
    /// rewriting the prefix and landing on a slug that only exists in Serbian.
    /// </summary>
    private void SetAlternates(
        IReadOnlyDictionary<string, string> categorySlugs,
        IReadOnlyDictionary<string, string>? productSlugs)
    {
        var alternates = new AlternateUrls();

        foreach (var code in CultureConstants.SupportedRouteCodes)
        {
            if (!categorySlugs.TryGetValue(code, out var categorySlug))
            {
                continue;
            }

            string? url;

            if (productSlugs is null)
            {
                url = Url.Action(nameof(Category), "Products",
                    new { culture = code, categorySlug });
            }
            else
            {
                if (!productSlugs.TryGetValue(code, out var productSlug))
                {
                    // The product has no translation in this language, so
                    // offering a link to it would only produce a 404.
                    continue;
                }

                url = Url.Action(nameof(Details), "Products",
                    new { culture = code, categorySlug, productSlug });
            }

            if (url is not null)
            {
                alternates.Set(code, url + CarriedQuery());
            }
        }

        ViewData[AlternateUrls.ViewDataKey] = alternates;
    }

    /// <summary>
    /// The current query string, minus anything that belongs in the path.
    ///
    /// Filters and paging should survive a language change — option codes and
    /// page numbers mean the same thing in both languages. Route values must
    /// not: carrying categorySlug across would append the old language's slug
    /// to a URL that already contains the new one.
    /// </summary>
    private string CarriedQuery()
    {
        var kept = Request.Query
            .Where(pair => !RouteValueKeys.Contains(pair.Key))
            .SelectMany(pair => pair.Value.Select(value => new KeyValuePair<string, string?>(pair.Key, value)))
            .ToList();

        if (kept.Count == 0)
        {
            return string.Empty;
        }

        var query = QueryString.Empty;

        foreach (var (key, value) in kept)
        {
            query = query.Add(key, value ?? string.Empty);
        }

        return query.ToUriComponent();
    }

    /// <summary>
    /// A slug that belongs to another language still resolves, but only after
    /// a permanent redirect to the address it has in the language being
    /// viewed. Anything genuinely unknown is a 404.
    /// </summary>
    private async Task<IActionResult> RescueCategoryAsync(
        string slug,
        string culture,
        CancellationToken ct)
    {
        // The slug of another language first, then one the category had
        // before the admin renamed it.
        var found = await _catalog.CategoryIdBySlugAsync(slug, ct)
            ?? await _catalog.FormerOwnerAsync(SlugOwner.Category, slug, culture, ct);

        if (found is not { } categoryId)
        {
            return NotFound();
        }

        var slugs = await _catalog.CategorySlugsAsync(categoryId, ct);

        return slugs.TryGetValue(culture, out var correct)
            ? RedirectToActionPermanent(nameof(Category), new { categorySlug = correct })
            : NotFound();
    }

    private async Task<IActionResult> RescueProductAsync(
        string slug,
        string culture,
        CancellationToken ct)
    {
        var found = await _catalog.ProductIdBySlugAsync(slug, ct)
            ?? await _catalog.FormerOwnerAsync(SlugOwner.Product, slug, culture, ct);

        if (found is not { } productId)
        {
            return NotFound();
        }

        var paths = await _catalog.ProductPathsAsync(productId, ct);

        return paths.TryGetValue(culture, out var path)
            ? RedirectToActionPermanent(nameof(Details), new
            {
                categorySlug = path.CategorySlug,
                productSlug = path.ProductSlug
            })
            : NotFound();
    }

    /// <summary>
    /// Attribute filters arrive under their own code as the key, because the
    /// set of them is data: ?color=black&amp;color=navy&amp;size=m. Model
    /// binding cannot know those names at compile time, so they are read off
    /// the request here and checked against the attributes that really exist.
    /// </summary>
    private async Task CollectAttributeFiltersAsync(ProductQuery query, CancellationToken ct)
    {
        var hasCandidates = Request.Query.Keys.Any(key => !ReservedQueryKeys.Contains(key));

        if (!hasCandidates)
        {
            return;
        }

        foreach (var code in await _catalog.FilterableAttributeCodesAsync(ct))
        {
            var values = Request.Query[code]
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (values.Count > 0)
            {
                query.Attributes[code] = values;
            }
        }
    }
}
