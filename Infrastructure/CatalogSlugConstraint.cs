using Microsoft.AspNetCore.Routing;

namespace WebShop.Infrastructure;

/// <summary>
/// Guards the catalog's slug routes, which sit at the same depth as the
/// controller routes: "/sr/haljine" and "/sr/cart" are both two segments.
/// Without this, "cart" would be a candidate category slug and the two
/// patterns would compete.
///
/// Anything listed here is a controller or area that owns its own URL, so it
/// can never also be a category slug. The category admin refuses these as
/// slugs for the same reason.
///
/// The catalog routes are registered first, so a controller missing from this
/// list loses its short URLs to them: /sr/addresses was answered by the
/// category page, and so were the review and address forms posting to
/// /sr/reviews/... and /sr/addresses/create. Add every new controller here.
/// </summary>
public sealed class CatalogSlugConstraint : IRouteConstraint
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "home", "products", "product", "cart", "checkout", "orders", "order",
        "account", "addresses", "wishlist", "reviews", "legal", "status", "seo",
        "identity", "admin", "error", "sitemap.xml", "robots.txt"
    };

    public static bool IsReserved(string slug) => Reserved.Contains(slug);

    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value))
        {
            return false;
        }

        var slug = Convert.ToString(value);

        return !string.IsNullOrWhiteSpace(slug) && !Reserved.Contains(slug);
    }
}
