using WebShop.Models.Catalog;

namespace WebShop.Models.Orders;

/// <summary>
/// A line on an order. Names, SKU and price are copied at checkout: if the
/// product is renamed, repriced or deleted, the order still reads correctly.
/// </summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    /// <summary>Kept for reporting. Null if the variant was later deleted.</summary>
    public int? ProductVariantId { get; set; }

    public ProductVariant? ProductVariant { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    /// <summary>Rendered variant description, e.g. "Crna / M" or "Warm Ivory 02 / 30 ml".</summary>
    public string? VariantDescription { get; set; }

    public string? ImagePath { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
