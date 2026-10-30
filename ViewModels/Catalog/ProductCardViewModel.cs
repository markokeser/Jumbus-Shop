namespace WebShop.ViewModels.Catalog;

/// <summary>
/// One product in a grid. Shared by the home page, the catalog and the
/// "related products" strip, so the card looks and behaves the same
/// everywhere and there is one place to change it.
/// </summary>
public class ProductCardViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string CategorySlug { get; set; } = string.Empty;

    public string? ShortDescription { get; set; }

    public string BrandName { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string? ImageAlt { get; set; }

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public bool IsInStock { get; set; }

    /// <summary>Published in the last 30 days.</summary>
    public bool IsNew { get; set; }

    /// <summary>Units left across active variants; the card says so when it is three or fewer.</summary>
    public int StockLeft { get; set; }

    /// <summary>Colour or shade swatches taken from this product's own variants.</summary>
    public IReadOnlyList<SwatchViewModel> Swatches { get; set; } = [];

    /// <summary>Text chips for products that vary only by volume, e.g. "50 ml".</summary>
    public IReadOnlyList<string> TextChips { get; set; } = [];

    /// <summary>
    /// How many distinct colours or shades exist, which can exceed the number
    /// of chips actually drawn. The view turns this into words, because
    /// pluralisation is a localisation problem.
    /// </summary>
    public int SwatchCount { get; set; }

    /// <summary>Size range as one token, e.g. "XS–L". Null when sizes do not apply.</summary>
    public string? SizeRun { get; set; }

    /// <summary>A non-variant note worth surfacing, such as the skin type.</summary>
    public string? Note { get; set; }

    public bool IsOnSale => SalePrice is { } sale && sale < Price;

    public decimal EffectivePrice => IsOnSale ? SalePrice!.Value : Price;

    public int DiscountPercent =>
        Price <= 0 ? 0 : (int)Math.Round((1 - EffectivePrice / Price) * 100);
}

public class SwatchViewModel
{
    public string Label { get; set; } = string.Empty;

    public string Hex { get; set; } = "#cccccc";

    /// <summary>Short form for the strip under a swatch, e.g. "01" from "01 Porcelan".</summary>
    public string ShortLabel =>
        Label.Split(' ', StringSplitOptions.RemoveEmptyEntries) is [var first, ..] ? first : Label;
}
