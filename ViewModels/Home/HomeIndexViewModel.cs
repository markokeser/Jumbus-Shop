using WebShop.ViewModels.Catalog;

namespace WebShop.ViewModels.Home;

public class HomeIndexViewModel
{
    public HeroViewModel Hero { get; set; } = new();

    /// <summary>Same card model the catalog uses, so both grids stay identical.</summary>
    public IReadOnlyList<ProductCardViewModel> Featured { get; set; } = [];

    public IReadOnlyList<CategoryGroupViewModel> CategoryGroups { get; set; } = [];

    public BuildStatusViewModel BuildStatus { get; set; } = new();
}

/// <summary>
/// The hero's right-hand panel is filled from the attribute tables rather than
/// hard-coded, so it keeps telling the truth as the catalogue changes.
/// </summary>
public class HeroViewModel
{
    public int VariantsInStock { get; set; }

    public decimal FreeShippingThreshold { get; set; }

    public int ReturnDays { get; set; }

    public string SizeAttributeName { get; set; } = string.Empty;

    public string ShadeAttributeName { get; set; } = string.Empty;

    public IReadOnlyList<string> SizeLabels { get; set; } = [];

    public IReadOnlyList<SwatchViewModel> Shades { get; set; } = [];

    public bool HasSpec => SizeLabels.Count > 0 || Shades.Count > 0;
}

public class CategoryGroupViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public IReadOnlyList<CategoryLinkViewModel> Children { get; set; } = [];
}

public class CategoryLinkViewModel
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int ProductCount { get; set; }
}

/// <summary>Shown in Development only.</summary>
public class BuildStatusViewModel
{
    public int Categories { get; set; }

    public int Products { get; set; }

    public int Variants { get; set; }

    public int InStock { get; set; }
}
