namespace WebShop.Models.Orders;

public class Coupon
{
    public int Id { get; set; }

    /// <summary>What the customer types at checkout. Compared case-insensitively.</summary>
    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    /// <summary>Percent when DiscountType is Percentage, otherwise an amount in RSD.</summary>
    public decimal Value { get; set; }

    /// <summary>Cart must reach this subtotal before the coupon applies.</summary>
    public decimal? MinimumSubtotal { get; set; }

    /// <summary>Caps a percentage discount, so "20% off" cannot become 40,000 RSD.</summary>
    public decimal? MaximumDiscount { get; set; }

    public DateTime? StartsUtc { get; set; }

    public DateTime? EndsUtc { get; set; }

    /// <summary>Null means unlimited.</summary>
    public int? UsageLimit { get; set; }

    public int TimesUsed { get; set; }

    /// <summary>Limits the coupon to one category tree, e.g. cosmetics only.</summary>
    public int? RestrictedToCategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = [];
}
