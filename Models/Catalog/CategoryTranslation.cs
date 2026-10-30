namespace WebShop.Models.Catalog;

public class CategoryTranslation
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    /// <summary>Short route code: "sr" or "en".</summary>
    public string Culture { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Translated slug, so /sr/haljine and /en/dresses point to the same category.</summary>
    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? MetaTitle { get; set; }

    public string? MetaDescription { get; set; }
}
