namespace WebShop.Models.Catalog;

/// <summary>
/// Says which attributes apply to a category. "Dresses" gets size and colour;
/// "Skincare" gets volume and skin type. This single table drives both the
/// admin entry form and the catalog filter panel, so the two can never drift apart.
/// </summary>
public class CategoryAttribute
{
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    /// <summary>Admin cannot save a variant without a value for this attribute.</summary>
    public bool IsRequired { get; set; } = true;

    public int DisplayOrder { get; set; }
}
