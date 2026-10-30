namespace WebShop.Models.Catalog;

public class ProductImage
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    /// <summary>Path relative to wwwroot, e.g. "media/products/haljina-01.jpg".</summary>
    public string Path { get; set; } = string.Empty;

    public string? ThumbnailPath { get; set; }

    /// <summary>Alt text is per language, so it lives here rather than in a translation table.</summary>
    public string? AltTextSr { get; set; }

    public string? AltTextEn { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }

    /// <summary>
    /// When set, this image belongs to one colour or shade, so the gallery
    /// can swap images as the customer picks a variant.
    /// </summary>
    public int? AttributeOptionId { get; set; }

    public AttributeOption? AttributeOption { get; set; }
}
