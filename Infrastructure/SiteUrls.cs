namespace WebShop.Infrastructure;

/// <summary>
/// Absolute addresses for canonical links, hreflang, the sitemap and social
/// previews — the places where a relative path is not enough.
/// </summary>
public static class SiteUrls
{
    /// <summary>
    /// The configured public address, or the current request's when none is
    /// set. Configuring it matters in production: behind a reverse proxy the
    /// request often arrives as http on an internal host name.
    /// </summary>
    public static string Origin(HttpContext context, ShopSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            return settings.BaseUrl.Trim().TrimEnd('/');
        }

        var request = context.Request;
        return $"{request.Scheme}://{request.Host}{request.PathBase}";
    }

    /// <summary>"/sr/haljine" or "media/products/1/a.jpg" made absolute; full URLs pass through.</summary>
    public static string Absolute(HttpContext context, ShopSettings settings, string pathOrUrl)
    {
        if (pathOrUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || pathOrUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return pathOrUrl;
        }

        return Origin(context, settings) + "/" + pathOrUrl.TrimStart('~', '/');
    }
}
