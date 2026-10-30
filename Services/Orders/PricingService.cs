using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Orders;
using WebShop.ViewModels.Cart;

namespace WebShop.Services.Orders;

public enum CouponProblem
{
    None = 0,
    NotFound = 1,
    Inactive = 2,
    NotStarted = 3,
    Expired = 4,
    UsedUp = 5,
    SubtotalTooLow = 6,
    NotApplicable = 7
}

public record CouponResult(Coupon? Coupon, CouponProblem Problem)
{
    public bool IsValid => Coupon is not null && Problem == CouponProblem.None;
}

public interface IPricingService
{
    Task<CouponResult> ValidateCouponAsync(string? code, IReadOnlyCollection<CartLineViewModel> lines, CancellationToken ct);

    PriceBreakdownViewModel Calculate(IEnumerable<CartLineViewModel> lines, Coupon? coupon);
}

public class PricingService : IPricingService
{
    private readonly ApplicationDbContext _context;
    private readonly ShopSettings _settings;

    public PricingService(ApplicationDbContext context, IOptions<ShopSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public async Task<CouponResult> ValidateCouponAsync(
        string? code,
        IReadOnlyCollection<CartLineViewModel> lines,
        CancellationToken ct)
    {
        var subtotal = lines.Sum(l => l.UnitPrice * l.Quantity);

        if (string.IsNullOrWhiteSpace(code))
        {
            return new CouponResult(null, CouponProblem.None);
        }

        var trimmed = code.Trim();

        var coupon = await _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == trimmed, ct);

        if (coupon is null)
        {
            return new CouponResult(null, CouponProblem.NotFound);
        }

        if (!coupon.IsActive)
        {
            return new CouponResult(null, CouponProblem.Inactive);
        }

        var now = DateTime.UtcNow;

        if (coupon.StartsUtc is { } starts && now < starts)
        {
            return new CouponResult(null, CouponProblem.NotStarted);
        }

        if (coupon.EndsUtc is { } ends && now > ends)
        {
            return new CouponResult(null, CouponProblem.Expired);
        }

        if (coupon.UsageLimit is { } limit && coupon.TimesUsed >= limit)
        {
            return new CouponResult(null, CouponProblem.UsedUp);
        }

        if (coupon.MinimumSubtotal is { } minimum && subtotal < minimum)
        {
            return new CouponResult(null, CouponProblem.SubtotalTooLow);
        }

        // A coupon limited to a category needs something from that category
        // in the basket. Until phase 5 this column was stored but never read,
        // so such a coupon discounted the whole basket.
        if (coupon.RestrictedToCategoryId is { } categoryId && !lines.Any(l => InCategory(l, categoryId)))
        {
            return new CouponResult(null, CouponProblem.NotApplicable);
        }

        return new CouponResult(coupon, CouponProblem.None);
    }

    /// <summary>
    /// Works out what the shopper pays.
    ///
    /// The order of operations matters and is not arbitrary:
    ///   1. subtotal from the lines, VAT already inside each price
    ///   2. discount, capped and never larger than the subtotal
    ///   3. shipping, which the free-delivery threshold reads from the
    ///      discounted figure — a coupon that drops the basket under the
    ///      threshold should also drop free delivery, or the discount is
    ///      being given twice
    ///   4. total
    ///   5. VAT extracted from the total, not added to it
    /// </summary>
    public PriceBreakdownViewModel Calculate(IEnumerable<CartLineViewModel> lines, Coupon? coupon)
    {
        var vatRate = _settings.VatRate;
        var subtotal = Round(lines.Sum(l => l.UnitPrice * l.Quantity));

        // A category coupon discounts only the lines from that category.
        var eligible = coupon?.RestrictedToCategoryId is { } categoryId
            ? Round(lines.Where(l => InCategory(l, categoryId)).Sum(l => l.UnitPrice * l.Quantity))
            : subtotal;

        var discount = CalculateDiscount(eligible, coupon);
        var discounted = subtotal - discount;

        var freeShippingByCoupon = coupon?.DiscountType == DiscountType.FreeShipping;

        var shipping = subtotal <= 0 || freeShippingByCoupon
            || discounted >= _settings.FreeShippingThreshold
                ? 0m
                : _settings.StandardShippingFee;

        var total = Round(discounted + shipping);

        // Retail prices already contain VAT, so it is pulled back out of the
        // total for the invoice. Adding vatRate on top here would overcharge
        // every customer by 20%.
        var vat = Round(total - (total / (1 + vatRate)));

        var remaining = shipping > 0
            ? Round(_settings.FreeShippingThreshold - discounted)
            : (decimal?)null;

        return new PriceBreakdownViewModel
        {
            Subtotal = subtotal,
            DiscountAmount = discount,
            ShippingFee = shipping,
            VatAmount = vat,
            VatRate = vatRate,
            Total = total,
            Currency = _settings.Currency,
            RemainingForFreeShipping = remaining is > 0 ? remaining : null
        };
    }

    /// <summary>The category itself or, for a top-level category such as Clothing, any of its children.</summary>
    public static bool InCategory(CartLineViewModel line, int categoryId) =>
        line.CategoryId == categoryId || line.ParentCategoryId == categoryId;

    private static decimal CalculateDiscount(decimal subtotal, Coupon? coupon)
    {
        if (coupon is null || subtotal <= 0)
        {
            return 0m;
        }

        var discount = coupon.DiscountType switch
        {
            DiscountType.Percentage => Round(subtotal * (coupon.Value / 100m)),
            DiscountType.FixedAmount => coupon.Value,

            // Free shipping is not a discount on the goods; it is handled by
            // waiving the shipping fee instead.
            _ => 0m
        };

        if (coupon.MaximumDiscount is { } cap && discount > cap)
        {
            discount = cap;
        }

        // A fixed-amount coupon larger than the basket must not produce a
        // negative total or hand back the difference.
        return Math.Min(Round(discount), subtotal);
    }

    /// <summary>
    /// Dinars are not divided further in practice, but the column is
    /// decimal(18,2) and percentage coupons produce fractions. Rounding to
    /// two places at every step keeps the stored figures adding up exactly.
    /// </summary>
    private static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
