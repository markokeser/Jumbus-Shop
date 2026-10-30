namespace WebShop.Models.Catalog;

public enum ProductStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

/// <summary>
/// Decides which column on VariantAttributeValue actually holds the value,
/// and how the attribute is rendered in the admin form and the filter panel.
/// </summary>
public enum AttributeDataType
{
    /// <summary>Pick from a fixed list: size, colour, shade.</summary>
    Option = 0,

    /// <summary>A number with a unit: volume in ml, weight in g.</summary>
    Numeric = 1,

    /// <summary>Free text: material composition, country of origin.</summary>
    Text = 2
}
