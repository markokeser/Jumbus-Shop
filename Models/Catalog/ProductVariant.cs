namespace WebShop.Models.Catalog;

/// <summary>
/// The thing a customer actually adds to the cart: one size in one colour,
/// or one shade in one volume. Price and stock live here, never on Product.
/// </summary>
public class ProductVariant
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    /// <summary>Full article number, unique across the shop.</summary>
    public string Sku { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal? SalePrice { get; set; }

    public DateTime? SaleStartsUtc { get; set; }

    public DateTime? SaleEndsUtc { get; set; }

    public int StockQuantity { get; set; }

    /// <summary>Below this level the admin dashboard flags the variant.</summary>
    public int LowStockThreshold { get; set; } = 3;

    /// <summary>
    /// Cosmetics only. A real column rather than an attribute, because it
    /// drives stock warnings rather than filtering or variant selection.
    /// </summary>
    public DateOnly? ExpiryDate { get; set; }

    /// <summary>Shipping weight in grams, used later for courier pricing.</summary>
    public int? WeightGrams { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<VariantAttributeValue> AttributeValues { get; set; } = [];

    /// <summary>
    /// Sale price only counts inside its window. Anything reading a price
    /// should go through here rather than checking SalePrice directly.
    /// </summary>
    public decimal GetEffectivePrice(DateTime? nowUtc = null)
    {
        if (SalePrice is not { } sale)
        {
            return Price;
        }

        var now = nowUtc ?? DateTime.UtcNow;

        if (SaleStartsUtc is { } start && now < start)
        {
            return Price;
        }

        if (SaleEndsUtc is { } end && now > end)
        {
            return Price;
        }

        return sale;
    }

    public bool IsOnSale(DateTime? nowUtc = null) =>
        GetEffectivePrice(nowUtc) < Price;

    public bool IsInStock => IsActive && StockQuantity > 0;
}
