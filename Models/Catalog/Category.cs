namespace WebShop.Models.Catalog;

/// <summary>
/// Hierarchical category. Top level has no parent (Clothing, Cosmetics);
/// children carry the actual products (Dresses, Skincare).
/// </summary>
public class Category
{
    public int Id { get; set; }

    public int? ParentId { get; set; }

    public Category? Parent { get; set; }

    /// <summary>Stable internal code, used by the seeder and by admin tooling.</summary>
    public string Code { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? ImagePath { get; set; }

    public ICollection<Category> Children { get; set; } = [];

    public ICollection<CategoryTranslation> Translations { get; set; } = [];

    public ICollection<Product> Products { get; set; } = [];

    /// <summary>Which attributes apply to products in this category.</summary>
    public ICollection<CategoryAttribute> CategoryAttributes { get; set; } = [];
}
