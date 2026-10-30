namespace WebShop.Models.Catalog;

public class AttributeOptionTranslation
{
    public int Id { get; set; }

    public int AttributeOptionId { get; set; }

    public AttributeOption AttributeOption { get; set; } = null!;

    public string Culture { get; set; } = string.Empty;

    /// <summary>
    /// Size labels are identical in both languages ("M" stays "M"), colours
    /// are not ("Crna" / "Black"). Both go through here so the rendering
    /// code never has to care which is which.
    /// </summary>
    public string Label { get; set; } = string.Empty;
}
