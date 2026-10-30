using Microsoft.AspNetCore.Localization;

namespace WebShop.Infrastructure;

/// <summary>
/// The remembered language. Pages without a language in the URL — the admin
/// panel, login redirects — read it through CookieRequestCultureProvider.
/// </summary>
public static class CultureCookie
{
    public static void Write(HttpContext context, string routeCode)
    {
        if (!CultureConstants.RouteToCulture.TryGetValue(routeCode, out var culture))
        {
            return;
        }

        var value = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));

        if (context.Request.Cookies.TryGetValue(CookieRequestCultureProvider.DefaultCookieName, out var existing)
            && existing == value)
        {
            return;
        }

        // A language preference is functional, not tracking: essential, so it
        // is set without waiting for cookie consent.
        context.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            value,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
    }
}
