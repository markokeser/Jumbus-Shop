using Microsoft.AspNetCore.Http;

namespace WebShop.Infrastructure;

/// <summary>
/// Rewrites the current URL to another language, keeping path and query
/// string intact. Used by the language switcher in _Layout.
/// </summary>
public static class CultureUrlHelper
{
    public static string SwitchCulture(HttpRequest request, string targetRouteCode)
    {
        var path = request.Path.Value ?? "/";
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length > 0 && CultureConstants.IsSupported(segments[0]))
        {
            segments[0] = targetRouteCode;
        }
        else
        {
            segments = [targetRouteCode, .. segments];
        }

        return "/" + string.Join('/', segments) + request.QueryString;
    }
}
