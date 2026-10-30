namespace WebShop.Models.Catalog;

/// <summary>
/// One allowed value of an Option-type attribute: the size "M",
/// the colour "Crna", the shade "Warm Ivory 02".
/// </summary>
public class AttributeOption
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    /// <summary>Stable code used in filter URLs: "m", "black", "warm-ivory-02".</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Hex colour for the swatch in the filter panel and variant picker.
    /// Set for colours and cosmetic shades, null for everything else.
    /// </summary>
    public string? SwatchHex { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<AttributeOptionTranslation> Translations { get; set; } = [];

    public ICollection<VariantAttributeValue> VariantValues { get; set; } = [];
}
