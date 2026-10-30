namespace WebShop.Models.Catalog;

/// <summary>
/// A product is the thing a customer browses. Everything that can differ
/// between two things a customer can actually buy — price, stock, size,
/// shade — lives on ProductVariant instead.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int? BrandId { get; set; }

    public Brand? Brand { get; set; }

    /// <summary>Base article number. Each variant appends its own suffix.</summary>
    public string Sku { get; set; } = string.Empty;

    public ProductStatus Status { get; set; } = ProductStatus.Draft;

    public bool IsFeatured { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? PublishedUtc { get; set; }

    public ICollection<ProductTranslation> Translations { get; set; } = [];

    public ICollection<ProductImage> Images { get; set; } = [];

    public ICollection<ProductVariant> Variants { get; set; } = [];
}
