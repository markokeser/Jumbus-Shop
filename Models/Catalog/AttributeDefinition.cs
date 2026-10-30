namespace WebShop.Models.Catalog;

/// <summary>
/// Describes one property a variant can have: size, colour, shade, volume.
/// Adding a new property to the shop means inserting a row here, not adding
/// a column — which is the whole point, since clothing and cosmetics need
/// entirely different sets.
/// </summary>
public class AttributeDefinition
{
    public int Id { get; set; }

    /// <summary>Stable code used in filter query strings: "size", "color", "shade", "volume".</summary>
    public string Code { get; set; } = string.Empty;

    public AttributeDataType DataType { get; set; } = AttributeDataType.Option;

    /// <summary>
    /// True when two variants differing only in this attribute are separate
    /// things to buy. Size and shade are variant-defining; country of origin is not.
    /// </summary>
    public bool IsVariantDefining { get; set; } = true;

    /// <summary>Whether the attribute appears in the catalog filter panel.</summary>
    public bool IsFilterable { get; set; } = true;

    /// <summary>Unit suffix for numeric attributes: "ml", "g". Null otherwise.</summary>
    public string? Unit { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<AttributeDefinitionTranslation> Translations { get; set; } = [];

    public ICollection<AttributeOption> Options { get; set; } = [];

    public ICollection<CategoryAttribute> CategoryAttributes { get; set; } = [];
}
