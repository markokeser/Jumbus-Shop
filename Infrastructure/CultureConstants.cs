using System.Globalization;

namespace WebShop.Infrastructure;

/// <summary>
/// Single source of truth for the languages the shop supports.
/// Add a language here and the route constraint, the language switcher
/// and the translation tables all pick it up.
/// </summary>
public static class CultureConstants
{
    public const string Serbian = "sr";
    public const string English = "en";

    public const string Default = Serbian;

    /// <summary>Route/UI code -> full culture used for formatting money and dates.</summary>
    public static readonly IReadOnlyDictionary<string, string> RouteToCulture =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Serbian] = "sr-Latn-RS",
            [English] = "en-US"
        };

    /// <summary>Names for the language switcher, each written in its own language.</summary>
    public static readonly IReadOnlyDictionary<string, string> DisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Serbian] = "Srpski",
            [English] = "English"
        };

    public static IReadOnlyList<string> SupportedRouteCodes { get; } = [Serbian, English];

    public static IReadOnlyList<CultureInfo> SupportedCultures { get; } =
        RouteToCulture.Values.Select(c => new CultureInfo(c)).ToList();

    public static bool IsSupported(string? routeCode) =>
        routeCode is not null && RouteToCulture.ContainsKey(routeCode);

    /// <summary>
    /// Turns a full culture ("sr-Latn-RS") back into the short route code ("sr").
    /// Falls back to the default language for anything unrecognised.
    /// </summary>
    public static string ToRouteCode(CultureInfo culture)
    {
        foreach (var (routeCode, cultureName) in RouteToCulture)
        {
            if (cultureName.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
            {
                return routeCode;
            }
        }

        var twoLetter = culture.TwoLetterISOLanguageName;
        return IsSupported(twoLetter) ? twoLetter : Default;
    }
}
