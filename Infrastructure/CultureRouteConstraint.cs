using Microsoft.AspNetCore.Routing;

namespace WebShop.Infrastructure;

/// <summary>
/// Registered as the "culture" route constraint so that "{culture:culture}/..."
/// only matches languages we actually serve. Without it, a request for
/// /favicon.ico would happily bind "favicon.ico" as a language code.
/// </summary>
public sealed class CultureRouteConstraint : IRouteConstraint
{
    public bool Match(
        HttpContext? httpContext,
        IRouter? route,
        string routeKey,
        RouteValueDictionary values,
        RouteDirection routeDirection)
    {
        if (!values.TryGetValue(routeKey, out var value) || value is null)
        {
            return false;
        }

        return CultureConstants.IsSupported(Convert.ToString(value));
    }
}
