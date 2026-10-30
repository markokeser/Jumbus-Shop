namespace WebShop.Infrastructure;

/// <summary>
/// The addresses of the current page in the other languages.
///
/// Catalog URLs carry translated slugs, so the language switcher cannot be
/// derived from the URL by swapping the prefix: /sr/haljine is /en/dresses,
/// not /en/haljine. Only the page knows its own counterparts, so it puts them
/// here and the layout reads them.
///
/// Pages whose path has no translated part — the home page, the cart, the
/// checkout — register nothing, and the layout falls back to swapping the
/// prefix, which is correct for them.
/// </summary>
public sealed class AlternateUrls
{
    public const string ViewDataKey = "AlternateUrls";

    private readonly Dictionary<string, string> _byCulture =
        new(StringComparer.OrdinalIgnoreCase);

    public void Set(string culture, string url) => _byCulture[culture] = url;

    public string? For(string culture) =>
        _byCulture.TryGetValue(culture, out var url) ? url : null;
}
