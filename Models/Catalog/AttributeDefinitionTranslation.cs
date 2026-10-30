namespace WebShop.Models.Catalog;

public class AttributeDefinitionTranslation
{
    public int Id { get; set; }

    public int AttributeDefinitionId { get; set; }

    public AttributeDefinition AttributeDefinition { get; set; } = null!;

    public string Culture { get; set; } = string.Empty;

    /// <summary>Label shown above the filter group and next to the variant picker.</summary>
    public string Name { get; set; } = string.Empty;
}
