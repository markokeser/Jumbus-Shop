namespace WebShop.Models.Catalog;

public class ProductTranslation
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public string Culture { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    /// <summary>One or two sentences for the product card and search results.</summary>
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }

    /// <summary>
    /// Fabric composition for clothing, INCI list for cosmetics.
    /// Kept separate from Description because it is searchable on its own —
    /// customers look for products without a specific ingredient.
    /// </summary>
    public string? Ingredients { get; set; }

    /// <summary>Washing instructions, or how to apply a cosmetic product.</summary>
    public string? CareInstructions { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }
}
