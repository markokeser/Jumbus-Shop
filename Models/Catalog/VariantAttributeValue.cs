namespace WebShop.Models.Catalog;

/// <summary>
/// One attribute value on one variant. Exactly one of the three value
/// columns is populated, chosen by the attribute's DataType.
/// </summary>
public class VariantAttributeValue
{
    public int Id { get; set; }

    public int ProductVariantId { get; set; }

    public ProductVariant ProductVariant { get; set; } = null!;

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    /// <summary>Set when DataType is Option.</summary>
    public int? AttributeOptionId { get; set; }

    public AttributeOption? AttributeOption { get; set; }

    /// <summary>Set when DataType is Numeric — volume, weight.</summary>
    public decimal? NumericValue { get; set; }

    /// <summary>Set when DataType is Text.</summary>
    public string? TextValue { get; set; }
}
