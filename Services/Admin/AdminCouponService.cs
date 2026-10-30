using System.Globalization;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Orders;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public interface IAdminCouponService
{
    Task<List<CouponRow>> ListAsync(string culture, CancellationToken ct);

    Task<CouponFormViewModel?> GetFormAsync(int id, string culture, CancellationToken ct);

    Task FillAsync(CouponFormViewModel form, string culture, CancellationToken ct);

    Task<SaveResult> SaveAsync(CouponFormViewModel form, CancellationToken ct);

    Task<DeleteOutcome> DeleteAsync(int id, CancellationToken ct);
}

public class AdminCouponService : IAdminCouponService
{
    private readonly ApplicationDbContext _context;

    public AdminCouponService(ApplicationDbContext context) => _context = context;

    public static CouponState StateOf(bool isActive, int timesUsed, int? limit, DateTime? startsUtc, DateTime? endsUtc, DateTime nowUtc) =>
        !isActive ? CouponState.Off
        : limit is { } l && timesUsed >= l ? CouponState.UsedUp
        : endsUtc is { } end && end < nowUtc ? CouponState.Expired
        : startsUtc is { } start && start > nowUtc ? CouponState.Scheduled
        : CouponState.Active;

    public async Task<List<CouponRow>> ListAsync(string culture, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var rows = await _context.Coupons
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Select(c => new CouponRow
            {
                Id = c.Id,
                Code = c.Code,
                DiscountType = c.DiscountType,
                Value = c.Value,
                MinimumSubtotal = c.MinimumSubtotal,
                StartsUtc = c.StartsUtc,
                EndsUtc = c.EndsUtc,
                TimesUsed = c.TimesUsed,
                UsageLimit = c.UsageLimit,
                IsActiveFlag = c.IsActive,
                Category = c.RestrictedToCategoryId == null
                    ? null
                    : _context.CategoryTranslations
                        .Where(t => t.CategoryId == c.RestrictedToCategoryId && t.Culture == culture)
                        .Select(t => t.Name)
                        .FirstOrDefault()
            })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            row.State = StateOf(row.IsActiveFlag, row.TimesUsed, row.UsageLimit, row.StartsUtc, row.EndsUtc, now);
        }

        return rows;
    }

    public async Task<CouponFormViewModel?> GetFormAsync(int id, string culture, CancellationToken ct)
    {
        var coupon = await _context.Coupons.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

        if (coupon is null)
        {
            return null;
        }

        var form = new CouponFormViewModel
        {
            Id = coupon.Id,
            Code = coupon.Code,
            DiscountType = coupon.DiscountType,
            Value = coupon.Value,
            MinimumSubtotal = coupon.MinimumSubtotal,
            MaximumDiscount = coupon.MaximumDiscount,
            StartsLocal = ShopTime.Local(coupon.StartsUtc),
            EndsLocal = ShopTime.Local(coupon.EndsUtc),
            UsageLimit = coupon.UsageLimit,
            RestrictedToCategoryId = coupon.RestrictedToCategoryId,
            IsActive = coupon.IsActive,
            TimesUsed = coupon.TimesUsed
        };

        await FillAsync(form, culture, ct);
        return form;
    }

    public async Task FillAsync(CouponFormViewModel form, string culture, CancellationToken ct)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.ParentId,
                c.DisplayOrder,
                Name = c.Translations.Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault() ?? c.Code
            })
            .ToListAsync(ct);

        // Top-level categories are offered too: a "Clothing" coupon covers
        // every subcategory under it (see PricingService.InCategory).
        form.Categories = [];

        foreach (var root in categories.Where(c => c.ParentId is null).OrderBy(c => c.DisplayOrder))
        {
            form.Categories.Add(new AdminOption(root.Id.ToString(CultureInfo.InvariantCulture), root.Name));

            foreach (var child in categories.Where(c => c.ParentId == root.Id).OrderBy(c => c.DisplayOrder))
            {
                form.Categories.Add(new AdminOption(child.Id.ToString(CultureInfo.InvariantCulture), $"{root.Name} › {child.Name}"));
            }
        }

        if (form.Id > 0)
        {
            form.TimesUsed = await _context.Coupons.Where(c => c.Id == form.Id).Select(c => c.TimesUsed).FirstOrDefaultAsync(ct);
        }
    }

    public async Task<SaveResult> SaveAsync(CouponFormViewModel form, CancellationToken ct)
    {
        var errors = new List<FieldError>();

        // Codes are typed by shoppers; one spelling avoids "LETO10" and "leto10"
        // being two different coupons on a case-sensitive collation later.
        var code = form.Code.Trim().ToUpperInvariant();

        if (await _context.Coupons.AnyAsync(c => c.Code == code && c.Id != form.Id, ct))
        {
            errors.Add(new(nameof(form.Code), "Admin_CouponTaken"));
        }

        switch (form.DiscountType)
        {
            case DiscountType.Percentage when form.Value is < 1 or > 100:
                errors.Add(new(nameof(form.Value), "Admin_PercentRange"));
                break;
            case DiscountType.FixedAmount when form.Value <= 0:
                errors.Add(new(nameof(form.Value), "Validation_Price"));
                break;
        }

        if (!Enum.IsDefined(form.DiscountType))
        {
            errors.Add(new(nameof(form.DiscountType), "Validation_Required"));
        }

        if (form.StartsLocal is { } starts && form.EndsLocal is { } ends && ends <= starts)
        {
            errors.Add(new(nameof(form.EndsLocal), "Admin_DatesOrder"));
        }

        if (form.RestrictedToCategoryId is { } categoryId && !await _context.Categories.AnyAsync(c => c.Id == categoryId, ct))
        {
            errors.Add(new(nameof(form.RestrictedToCategoryId), "Validation_Required"));
        }

        Coupon? coupon = null;

        if (form.Id > 0)
        {
            coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == form.Id, ct);

            if (coupon is null)
            {
                return SaveResult.Missing();
            }
        }

        if (errors.Count > 0)
        {
            return SaveResult.Fail(errors);
        }

        if (coupon is null)
        {
            coupon = new Coupon();
            _context.Coupons.Add(coupon);
        }

        coupon.Code = code;
        coupon.DiscountType = form.DiscountType;
        coupon.Value = form.DiscountType == DiscountType.FreeShipping ? 0 : form.Value;
        coupon.MinimumSubtotal = form.MinimumSubtotal;
        coupon.MaximumDiscount = form.DiscountType == DiscountType.Percentage ? form.MaximumDiscount : null;
        coupon.StartsUtc = ShopTime.ToUtc(form.StartsLocal);
        coupon.EndsUtc = ShopTime.ToUtc(form.EndsLocal);
        coupon.UsageLimit = form.UsageLimit;
        coupon.RestrictedToCategoryId = form.RestrictedToCategoryId;
        coupon.IsActive = form.IsActive;

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return SaveResult.Fail([new(nameof(form.Code), "Admin_CouponTaken")]);
        }

        return SaveResult.Ok(coupon.Id);
    }

    /// <summary>A used coupon stays: orders keep pointing at it. Switch it off instead.</summary>
    public async Task<DeleteOutcome> DeleteAsync(int id, CancellationToken ct)
    {
        var coupon = await _context.Coupons.FirstOrDefaultAsync(c => c.Id == id, ct);

        if (coupon is null)
        {
            return DeleteOutcome.NotFound;
        }

        if (coupon.TimesUsed > 0 || await _context.Orders.AnyAsync(o => o.CouponId == id, ct))
        {
            return DeleteOutcome.InUse;
        }

        _context.Coupons.Remove(coupon);
        await _context.SaveChangesAsync(ct);
        return DeleteOutcome.Deleted;
    }
}
