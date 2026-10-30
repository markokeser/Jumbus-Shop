using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Models.Orders;
using WebShop.Services.Orders;
using WebShop.ViewModels.Cart;

namespace WebShop.Services.Cart;

public enum AddToCartResult
{
    Added = 0,
    QuantityCapped = 1,
    OutOfStock = 2,
    NotFound = 3
}

public interface ICartService
{
    Task<CartViewModel> GetAsync(string culture, CancellationToken ct);

    Task<AddToCartResult> AddAsync(int variantId, int quantity, CancellationToken ct);

    Task SetQuantityAsync(int variantId, int quantity, CancellationToken ct);

    void Remove(int variantId);

    void Clear();

    int ItemCount();

    string? CouponCode { get; set; }
}

/// <summary>
/// The cart lives in session, so a guest can shop without an account.
///
/// Only variant ids and quantities are stored. Price, stock and name are read
/// fresh from the database on every request, which means a cart left open
/// overnight can never check out at yesterday's price or buy something that
/// has since sold out.
/// </summary>
public class CartService : ICartService
{
    private const string ItemsKey = "cart.items";
    private const string CouponKey = "cart.coupon";

    /// <summary>A sane ceiling per line; anything more is a wholesale order.</summary>
    private const int MaxPerLine = 20;

    private readonly ApplicationDbContext _context;
    private readonly IPricingService _pricing;
    private readonly IHttpContextAccessor _http;

    public CartService(
        ApplicationDbContext context,
        IPricingService pricing,
        IHttpContextAccessor http)
    {
        _context = context;
        _pricing = pricing;
        _http = http;
    }

    private ISession Session =>
        _http.HttpContext?.Session
        ?? throw new InvalidOperationException(
            "The cart needs session state. Check that UseSession runs before the endpoints.");

    public string? CouponCode
    {
        get => Session.GetString(CouponKey);
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Session.Remove(CouponKey);
            }
            else
            {
                Session.SetString(CouponKey, value.Trim());
            }
        }
    }

    // ------------------------------------------------------------ storage

    private List<CartItem> Read()
    {
        var raw = Session.GetString(ItemsKey);

        if (string.IsNullOrEmpty(raw))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CartItem>>(raw) ?? [];
        }
        catch (JsonException)
        {
            // A cookie left over from an older shape of this class should not
            // take the page down; an empty cart is the safe reading.
            Session.Remove(ItemsKey);
            return [];
        }
    }

    private void Write(List<CartItem> items)
    {
        if (items.Count == 0)
        {
            Session.Remove(ItemsKey);
            return;
        }

        Session.SetString(ItemsKey, JsonSerializer.Serialize(items));
    }

    public int ItemCount() => Read().Sum(i => i.Quantity);

    public void Remove(int variantId)
    {
        var items = Read();
        items.RemoveAll(i => i.ProductVariantId == variantId);
        Write(items);
    }

    public void Clear()
    {
        Session.Remove(ItemsKey);
        Session.Remove(CouponKey);
    }

    // ------------------------------------------------------------ changes

    public async Task<AddToCartResult> AddAsync(int variantId, int quantity, CancellationToken ct)
    {
        if (quantity < 1)
        {
            quantity = 1;
        }

        var stock = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId && v.IsActive)
            .Select(v => (int?)v.StockQuantity)
            .FirstOrDefaultAsync(ct);

        if (stock is null)
        {
            return AddToCartResult.NotFound;
        }

        if (stock <= 0)
        {
            return AddToCartResult.OutOfStock;
        }

        var items = Read();
        var line = items.FirstOrDefault(i => i.ProductVariantId == variantId);
        var wanted = (line?.Quantity ?? 0) + quantity;
        var allowed = Math.Min(Math.Min(wanted, stock.Value), MaxPerLine);

        if (line is null)
        {
            items.Add(new CartItem { ProductVariantId = variantId, Quantity = allowed });
        }
        else
        {
            line.Quantity = allowed;
        }

        Write(items);

        return allowed < wanted ? AddToCartResult.QuantityCapped : AddToCartResult.Added;
    }

    public async Task SetQuantityAsync(int variantId, int quantity, CancellationToken ct)
    {
        if (quantity < 1)
        {
            Remove(variantId);
            return;
        }

        var stock = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId && v.IsActive)
            .Select(v => (int?)v.StockQuantity)
            .FirstOrDefaultAsync(ct);

        if (stock is null or <= 0)
        {
            Remove(variantId);
            return;
        }

        var items = Read();
        var line = items.FirstOrDefault(i => i.ProductVariantId == variantId);

        if (line is null)
        {
            return;
        }

        line.Quantity = Math.Min(Math.Min(quantity, stock.Value), MaxPerLine);
        Write(items);
    }

    // ------------------------------------------------------------ reading

    public async Task<CartViewModel> GetAsync(string culture, CancellationToken ct)
    {
        var items = Read();
        var model = new CartViewModel { CouponCode = CouponCode };

        if (items.Count == 0)
        {
            model.Totals = _pricing.Calculate([], null);
            return model;
        }

        var ids = items.Select(i => i.ProductVariantId).ToList();
        var now = DateTime.UtcNow;

        var rows = await _context.ProductVariants
            .AsNoTracking()
            .Where(v => ids.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                v.Sku,
                v.IsActive,
                v.StockQuantity,
                v.Price,
                SalePrice = v.SalePrice != null
                    && v.SalePrice < v.Price
                    && (v.SaleStartsUtc == null || v.SaleStartsUtc <= now)
                    && (v.SaleEndsUtc == null || v.SaleEndsUtc >= now)
                        ? v.SalePrice
                        : null,
                ProductId = v.ProductId,
                CategoryId = v.Product.CategoryId,
                ParentCategoryId = v.Product.Category.ParentId,
                ProductStatus = v.Product.Status,
                ProductName = v.Product.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Name).FirstOrDefault(),
                ProductSlug = v.Product.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault(),
                CategorySlug = v.Product.Category.Translations
                    .Where(t => t.Culture == culture).Select(t => t.Slug).FirstOrDefault(),
                BrandName = v.Product.Brand != null ? v.Product.Brand.Name : string.Empty,
                ImagePath = v.Product.Images
                    .Where(i => i.IsPrimary).Select(i => i.Path).FirstOrDefault(),
                Values = v.AttributeValues
                    .Where(av => av.AttributeDefinition.IsVariantDefining)
                    .OrderBy(av => av.AttributeDefinition.DisplayOrder)
                    .Select(av => new
                    {
                        av.NumericValue,
                        Unit = av.AttributeDefinition.Unit,
                        Hex = av.AttributeOption != null ? av.AttributeOption.SwatchHex : null,
                        Label = av.AttributeOption == null
                            ? null
                            : av.AttributeOption.Translations
                                .Where(t => t.Culture == culture).Select(t => t.Label).FirstOrDefault()
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var byId = rows.ToDictionary(r => r.Id);
        var changed = false;

        foreach (var item in items)
        {
            if (!byId.TryGetValue(item.ProductVariantId, out var row)
                || !row.IsActive
                || row.ProductStatus != Models.Catalog.ProductStatus.Published)
            {
                model.Dropped.Add(new CartLineViewModel
                {
                    VariantId = item.ProductVariantId,
                    ProductName = row?.ProductName ?? string.Empty,
                    Sku = row?.Sku ?? string.Empty,
                    Notice = CartNotice.Removed
                });
                changed = true;
                continue;
            }

            var unit = row.SalePrice ?? row.Price;

            var line = new CartLineViewModel
            {
                VariantId = row.Id,
                ProductId = row.ProductId,
                CategoryId = row.CategoryId,
                ParentCategoryId = row.ParentCategoryId,
                ProductName = row.ProductName ?? row.Sku,
                ProductSlug = row.ProductSlug ?? string.Empty,
                CategorySlug = row.CategorySlug ?? string.Empty,
                BrandName = row.BrandName,
                Sku = row.Sku,
                ImagePath = row.ImagePath,
                SwatchHex = row.Values.Select(v => v.Hex).FirstOrDefault(h => h != null),
                UnitPrice = unit,
                ListPrice = row.SalePrice is null ? null : row.Price,
                RequestedQuantity = item.Quantity,
                Quantity = item.Quantity,
                Available = row.StockQuantity,
                VariantDescription = Describe(row.Values.Select(v =>
                    v.Label ?? FormatNumeric(v.NumericValue, v.Unit)))
            };

            if (row.StockQuantity <= 0)
            {
                line.Quantity = 0;
                line.Notice = CartNotice.OutOfStock;
                changed = true;
            }
            else if (item.Quantity > row.StockQuantity)
            {
                line.Quantity = row.StockQuantity;
                line.Notice = CartNotice.QuantityReduced;
                changed = true;
            }

            model.Lines.Add(line);
        }

        if (changed)
        {
            // Write the corrected quantities straight back, so the cart the
            // shopper sees and the cart in session never disagree.
            Write([.. model.Lines
                .Where(l => l.Quantity > 0)
                .Select(l => new CartItem { ProductVariantId = l.VariantId, Quantity = l.Quantity })]);
        }

        var coupon = await _pricing.ValidateCouponAsync(CouponCode, model.Lines, ct);

        if (!string.IsNullOrWhiteSpace(CouponCode) && !coupon.IsValid)
        {
            model.CouponProblem = coupon.Problem.ToString();
        }

        model.Totals = _pricing.Calculate(model.Lines, coupon.Coupon);

        return model;
    }

    private static string? Describe(IEnumerable<string?> parts)
    {
        var joined = string.Join(" / ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return string.IsNullOrEmpty(joined) ? null : joined;
    }

    private static string? FormatNumeric(decimal? value, string? unit) =>
        value is null ? null : $"{value.Value.ToString("0.##", CultureInfo.CurrentCulture)} {unit}".Trim();
}
