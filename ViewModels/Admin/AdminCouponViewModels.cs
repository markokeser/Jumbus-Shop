using System.ComponentModel.DataAnnotations;
using WebShop.Models.Orders;

namespace WebShop.ViewModels.Admin;

public enum CouponState
{
    Active = 0,
    Scheduled = 1,
    Expired = 2,
    UsedUp = 3,
    Off = 4
}

public class CouponRow
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; }

    public decimal Value { get; set; }

    public decimal? MinimumSubtotal { get; set; }

    public DateTime? StartsUtc { get; set; }

    public DateTime? EndsUtc { get; set; }

    public int TimesUsed { get; set; }

    public int? UsageLimit { get; set; }

    public string? Category { get; set; }

    public bool IsActiveFlag { get; set; }

    public CouponState State { get; set; }
}

public class CouponFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Validation_Required")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Validation_CouponCode")]
    [RegularExpression("^[A-Za-z0-9-]+$", ErrorMessage = "Validation_CouponCode")]
    public string Code { get; set; } = string.Empty;

    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

    /// <summary>Percent for a percentage coupon, dinars for a fixed one, ignored for free delivery.</summary>
    public decimal Value { get; set; }

    [Range(typeof(decimal), "0", "100000000", ErrorMessage = "Validation_Price")]
    public decimal? MinimumSubtotal { get; set; }

    [Range(typeof(decimal), "1", "100000000", ErrorMessage = "Validation_Price")]
    public decimal? MaximumDiscount { get; set; }

    public DateTime? StartsLocal { get; set; }

    public DateTime? EndsLocal { get; set; }

    [Range(1, 1_000_000, ErrorMessage = "Validation_UsageLimit")]
    public int? UsageLimit { get; set; }

    public int? RestrictedToCategoryId { get; set; }

    public bool IsActive { get; set; } = true;

    // Display only.
    public int TimesUsed { get; set; }

    public List<AdminOption> Categories { get; set; } = [];

    public bool IsNew => Id == 0;
}
