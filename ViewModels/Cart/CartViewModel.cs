namespace WebShop.ViewModels.Cart;

/// <summary>
/// Why a line changed between the shopper putting it in the cart and the cart
/// being shown again. Prices move, stock runs out, products get withdrawn —
/// all of which has to be said out loud rather than silently applied.
/// </summary>
public enum CartNotice
{
    None = 0,
    QuantityReduced = 1,
    OutOfStock = 2,
    Removed = 3,
    PriceChanged = 4
}

public class CartLineViewModel
{
    public int VariantId { get; set; }

    public int ProductId { get; set; }

    /// <summary>For coupons limited to a category: the line's category and its parent.</summary>
    public int CategoryId { get; set; }

    public int? ParentCategoryId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSlug { get; set; } = string.Empty;

    public string CategorySlug { get; set; } = string.Empty;

    public string BrandName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    /// <summary>Rendered variant description, e.g. "Crna / M".</summary>
    public string? VariantDescription { get; set; }

    public string? ImagePath { get; set; }

    /// <summary>First swatch of the chosen variant, so the line has something to look at.</summary>
    public string? SwatchHex { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Set only when this line is discounted, for the struck-through price.</summary>
    public decimal? ListPrice { get; set; }

    public int Quantity { get; set; }

    public int Available { get; set; }

    public CartNotice Notice { get; set; }

    /// <summary>What the quantity was before stock forced it down.</summary>
    public int RequestedQuantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;

    public bool IsDiscounted => ListPrice is { } list && list > UnitPrice;
}

public class CartViewModel
{
    public List<CartLineViewModel> Lines { get; set; } = [];

    /// <summary>Lines dropped entirely, kept so the page can explain the gap.</summary>
    public List<CartLineViewModel> Dropped { get; set; } = [];

    public PriceBreakdownViewModel Totals { get; set; } = new();

    public string? CouponCode { get; set; }

    /// <summary>Set when a coupon was entered but does not apply.</summary>
    public string? CouponProblem { get; set; }

    public bool IsEmpty => Lines.Count == 0;

    public int ItemCount => Lines.Sum(l => l.Quantity);

    public bool HasNotices =>
        Dropped.Count > 0 || Lines.Any(l => l.Notice != CartNotice.None);

    /// <summary>Nothing may be ordered unless every line can actually be supplied.</summary>
    public bool CanCheckout =>
        Lines.Count > 0 && Lines.All(l => l.Quantity > 0 && l.Available >= l.Quantity);
}

/// <summary>
/// The money. Every figure is computed once, here, and then stored on the
/// order — never recomputed on read, because prices, VAT rates and shipping
/// rules change and an old order has to keep saying what was actually charged.
/// </summary>
public class PriceBreakdownViewModel
{
    /// <summary>Sum of the lines, VAT included, before any discount.</summary>
    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    /// <summary>
    /// VAT contained in the total, not added to it. Retail prices in Serbia
    /// are shown to consumers with VAT already inside, so this is extracted
    /// for the invoice rather than charged on top.
    /// </summary>
    public decimal VatAmount { get; set; }

    public decimal VatRate { get; set; }

    public decimal Total { get; set; }

    public string Currency { get; set; } = "RSD";

    /// <summary>How much more is needed for free delivery; null once it is reached.</summary>
    public decimal? RemainingForFreeShipping { get; set; }

    public bool HasDiscount => DiscountAmount > 0;

    public bool IsShippingFree => ShippingFee <= 0;
}
