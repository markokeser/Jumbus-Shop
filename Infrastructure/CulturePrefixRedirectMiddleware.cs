using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace WebShop.Infrastructure;

/// <summary>
/// Sends "/" and "/products" to "/sr/" and "/sr/products", so every public
/// page has exactly one address and search engines never see the same content
/// under two URLs. The language is whatever request localisation already
/// resolved — the visitor's cookie, then their browser, then Serbian.
/// </summary>
public sealed class CulturePrefixRedirectMiddleware
{
    /// <summary>
    /// Paths that legitimately live outside the language tree. Admin is an
    /// internal tool, Identity ships its own routes, and the rest are files
    /// or crawler endpoints that must stay at a fixed address.
    /// </summary>
    private static readonly string[] ExcludedPrefixes =
    [
        "/admin",
        "/identity",
        "/css",
        "/js",
        "/lib",
        "/media",
        "/images",
        "/favicon",
        "/robots.txt",
        "/sitemap.xml",
        "/fonts",
        "/status/",
        "/.well-known"
    ];

    private readonly RequestDelegate _next;

    public CulturePrefixRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (IsExcluded(path))
        {
            await _next(context);
            return;
        }

        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length > 0 && CultureConstants.IsSupported(segments[0]))
        {
            RememberCulture(context, segments[0]);
            await _next(context);
            return;
        }

        var routeCode = CultureConstants.ToRouteCode(CultureInfo.CurrentUICulture);
        var target = $"/{routeCode}{path}{context.Request.QueryString}";

        // 302 rather than 301: a permanent redirect gets cached in the
        // visitor's browser, which makes changing this rule later painful.
        context.Response.Redirect(target);
    }

    /// <summary>
    /// Stores the language of the page being viewed, so that pages without a
    /// language prefix — a redirect to "/", a login path, the Identity
    /// fallbacks — open in the language the shopper last chose rather than
    /// in whatever the browser happens to ask for.
    ///
    /// The phase 1 plan promised this and it was never written; the account
    /// pages are where its absence would first have shown.
    /// </summary>
    private static void RememberCulture(HttpContext context, string routeCode) =>
        CultureCookie.Write(context, routeCode);

    private static bool IsExcluded(string path)
    {
        foreach (var prefix in ExcludedPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
