using WebShop.Services.Catalog;

namespace WebShop.ViewModels.Catalog;

public class ProductListViewModel
{
    public ProductQuery Query { get; set; } = new();

    public IReadOnlyList<ProductCardViewModel> Products { get; set; } = [];

    public FilterPanelViewModel Filters { get; set; } = new();

    public int TotalCount { get; set; }

    public int PageCount { get; set; }

    /// <summary>Set when the page is a category listing rather than a search.</summary>
    public CategoryContext? Category { get; set; }

    public bool HasResults => Products.Count > 0;

    public bool HasPreviousPage => Query.Page > 1;

    public bool HasNextPage => Query.Page < PageCount;

    /// <summary>
    /// Search results and filtered views must not be indexed: they multiply
    /// into thousands of near-identical URLs. A plain category page should be.
    /// </summary>
    public bool ShouldNoIndex => Query.HasAnyFilter || Query.Page > 1;
}

public class FilterPanelViewModel
{
    public IReadOnlyList<FilterGroupViewModel> Groups { get; set; } = [];

    public IReadOnlyList<FilterOptionViewModel> Brands { get; set; } = [];

    public decimal LowestPrice { get; set; }

    public decimal HighestPrice { get; set; }

    public bool HasAnything => Groups.Count > 0 || Brands.Count > 0;
}

public class FilterGroupViewModel
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Unit { get; set; }

    public List<FilterOptionViewModel> Options { get; set; } = [];

    /// <summary>Colour and shade render as swatches rather than checkboxes.</summary>
    public bool IsSwatchGroup => Options.Any(o => o.SwatchHex is not null);
}

public class FilterOptionViewModel
{
    public string Code { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string? SwatchHex { get; set; }

    public bool IsSelected { get; set; }
}
