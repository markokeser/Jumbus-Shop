namespace WebShop.Models.Catalog;

/// <summary>
/// Brand names are proper nouns, so they need no translation table —
/// only the description does, and that is rarely shown. Kept flat on purpose.
/// </summary>
public class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? LogoPath { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = [];
}
