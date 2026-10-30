using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace WebShop.Services.Catalog;

public enum ProductSort
{
    Newest = 0,
    PriceLowToHigh = 1,
    PriceHighToLow = 2,
    NameAscending = 3
}

/// <summary>
/// Everything the catalog page can be narrowed by. Bound straight from the
/// query string, so the URL is the whole state of the page — which makes the
/// filtered view shareable, bookmarkable and cacheable.
///
/// Attribute filters arrive as repeated keys named after the attribute code:
///   ?color=black&amp;color=navy&amp;size=m
/// That is collected by <see cref="Attributes"/> rather than by model binding,
/// because the set of attributes is data, not something known at compile time.
/// </summary>
public class ProductQuery
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 48;

    [FromQuery(Name = "q")]
    public string? Search { get; set; }

    [FromQuery(Name = "brand")]
    public List<string> Brands { get; set; } = [];

    [FromQuery(Name = "min")]
    public decimal? MinPrice { get; set; }

    [FromQuery(Name = "max")]
    public decimal? MaxPrice { get; set; }

    [FromQuery(Name = "stock")]
    public bool InStockOnly { get; set; }

    [FromQuery(Name = "sale")]
    public bool OnSaleOnly { get; set; }

    [FromQuery(Name = "sort")]
    public ProductSort Sort { get; set; } = ProductSort.Newest;

    [FromQuery(Name = "page")]
    public int Page { get; set; } = 1;

    /// <summary>Not bound from the query string; set by the controller from the route.</summary>
    [BindNever]
    public int? CategoryId { get; set; }

    /// <summary>Attribute code to selected option codes. Filled by the controller.</summary>
    [BindNever]
    public Dictionary<string, List<string>> Attributes { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public int PageSize { get; set; } = DefaultPageSize;

    public bool HasSearch => !string.IsNullOrWhiteSpace(Search);

    public bool HasAnyFilter =>
        HasSearch
        || Brands.Count > 0
        || MinPrice is not null
        || MaxPrice is not null
        || InStockOnly
        || OnSaleOnly
        || Attributes.Count > 0;

    /// <summary>Clamps anything a hand-edited URL could push out of range.</summary>
    public void Normalise()
    {
        Page = Page < 1 ? 1 : Page;
        PageSize = PageSize is < 1 or > MaxPageSize ? DefaultPageSize : PageSize;

        if (MinPrice is < 0) MinPrice = null;
        if (MaxPrice is < 0) MaxPrice = null;

        if (MinPrice is { } min && MaxPrice is { } max && min > max)
        {
            (MinPrice, MaxPrice) = (max, min);
        }

        Brands = [.. Brands.Where(b => !string.IsNullOrWhiteSpace(b)).Distinct()];
    }

    /// <summary>
    /// The current filters as query-string pairs, duplicates included.
    ///
    /// A dictionary cannot express this: colour alone can appear several
    /// times (?color=black&amp;color=navy), and that repeated form is exactly
    /// what the controller reads back out of Request.Query. Indexing the keys
    /// instead — color[0], color[1] — produces a URL that binds to nothing,
    /// so every attribute filter would vanish the moment someone paged.
    ///
    /// Numbers are written with the invariant culture because that is what
    /// query-string model binding reads them with; a Serbian decimal comma
    /// here would come back as a different number or as null.
    /// </summary>
    public List<KeyValuePair<string, string>> ToQueryPairs(bool includeSort = true)
    {
        var pairs = new List<KeyValuePair<string, string>>();

        void Add(string key, string value) => pairs.Add(new(key, value));

        if (HasSearch) Add("q", Search!);
        if (MinPrice is { } min) Add("min", min.ToString("0.##", CultureInfo.InvariantCulture));
        if (MaxPrice is { } max) Add("max", max.ToString("0.##", CultureInfo.InvariantCulture));
        if (InStockOnly) Add("stock", "true");
        if (OnSaleOnly) Add("sale", "true");
        if (includeSort && Sort != ProductSort.Newest) Add("sort", Sort.ToString());

        foreach (var brand in Brands)
        {
            Add("brand", brand);
        }

        foreach (var (code, selected) in Attributes)
        {
            foreach (var value in selected)
            {
                Add(code, value);
            }
        }

        return pairs;
    }

    /// <summary>
    /// The filters as a query string, optionally overriding the page. Pass
    /// this alongside Url.Action rather than using asp-all-route-data, which
    /// takes a dictionary and therefore cannot carry repeated keys.
    /// </summary>
    public QueryString ToQueryString(int? page = null)
    {
        var result = QueryString.Empty;

        foreach (var (key, value) in ToQueryPairs())
        {
            result = result.Add(key, value);
        }

        var target = page ?? Page;

        if (target > 1)
        {
            result = result.Add("page", target.ToString(CultureInfo.InvariantCulture));
        }

        return result;
    }
}
