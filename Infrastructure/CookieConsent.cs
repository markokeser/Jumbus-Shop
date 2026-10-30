namespace WebShop.Infrastructure;

public enum ConsentChoice
{
    /// <summary>Only what the site needs to work: sign-in, cart, language, form security.</summary>
    Necessary = 1,

    /// <summary>Also analytics and marketing, once the shop adds any.</summary>
    All = 2
}

/// <summary>
/// The visitor's cookie choice, kept in a cookie of its own.
///
/// It is also the cookie ASP.NET Core's cookie policy reads (see Program.cs):
/// only the value "all" counts as consent there, so any cookie not marked
/// essential is dropped until the visitor accepts. "necessary" records that
/// the question was answered, so the banner does not come back.
///
/// Today the shop sets necessary cookies only. Anything added later —
/// analytics, a pixel — belongs in the CanTrack block at the end of _Layout.
/// </summary>
public static class CookieConsent
{
    public const string CookieName = ".WebShop.Consent";

    public const string AllValue = "all";

    public const string NecessaryValue = "necessary";

    /// <summary>Six months, then the visitor is asked again.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(182);

    public static ConsentChoice? Read(HttpContext context) =>
        context.Request.Cookies[CookieName] switch
        {
            AllValue => ConsentChoice.All,
            NecessaryValue => ConsentChoice.Necessary,
            _ => null
        };

    public static void Write(HttpContext context, ConsentChoice choice) =>
        context.Response.Cookies.Append(
            CookieName,
            choice == ConsentChoice.All ? AllValue : NecessaryValue,
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.Add(Lifetime),
                IsEssential = true,
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Secure = context.Request.IsHttps
            });
}
