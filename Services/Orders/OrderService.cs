using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Orders;
using WebShop.Services.Cart;
using WebShop.Services.Email;
using WebShop.Services.Payment;
using WebShop.ViewModels.Checkout;

namespace WebShop.Services.Orders;

public enum PlaceOrderProblem
{
    None = 0,
    CartEmpty = 1,
    StockGone = 2,
    PaymentDeclined = 3
}

public record PlaceOrderResult(
    Order? Order,
    PlaceOrderProblem Problem,
    string? OffendingProduct = null)
{
    public bool Succeeded => Order is not null && Problem == PlaceOrderProblem.None;
}

public interface IOrderService
{
    Task<PlaceOrderResult> PlaceAsync(
        CheckoutFormViewModel form,
        string culture,
        string? userId,
        CancellationToken ct);

    /// <summary>An order belonging to this signed-in customer, and nobody else.</summary>
    Task<Order?> FindOwnedAsync(string orderNumber, string userId, CancellationToken ct);

    /// <summary>
    /// A guest looking up their own order. Both the number and the email have
    /// to match: the number alone is sequential and trivially guessed.
    /// </summary>
    Task<Order?> FindForGuestAsync(string orderNumber, string email, CancellationToken ct);

    /// <summary>An order placed from this browser session — how a guest reaches the confirmation page.</summary>
    Task<Order?> FindPlacedThisSessionAsync(string orderNumber, CancellationToken ct);

    Task<List<Order>> ListForUserAsync(string userId, CancellationToken ct);
}

public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _context;
    private readonly ICartService _cart;
    private readonly IPricingService _pricing;
    private readonly IEnumerable<IPaymentProvider> _payments;
    private readonly IOrderEmailSender _email;
    private readonly ShopSettings _settings;
    private readonly ILogger<OrderService> _logger;
    private readonly IHttpContextAccessor _http;

    /// <summary>Session key holding the numbers of orders placed in this session.</summary>
    private const string PlacedKey = "orders.placed";

    public OrderService(
        ApplicationDbContext context,
        ICartService cart,
        IPricingService pricing,
        IEnumerable<IPaymentProvider> payments,
        IOrderEmailSender email,
        IOptions<ShopSettings> settings,
        ILogger<OrderService> logger,
        IHttpContextAccessor http)
    {
        _context = context;
        _cart = cart;
        _pricing = pricing;
        _payments = payments;
        _email = email;
        _settings = settings.Value;
        _logger = logger;
        _http = http;
    }

    public async Task<PlaceOrderResult> PlaceAsync(
        CheckoutFormViewModel form,
        string culture,
        string? userId,
        CancellationToken ct)
    {
        var cart = await _cart.GetAsync(culture, ct);

        if (!cart.CanCheckout)
        {
            return new PlaceOrderResult(null, PlaceOrderProblem.CartEmpty);
        }

        // Prices and the discount are recomputed here from the database.
        // Nothing about money is taken from the submitted form — the form is
        // only ever the shopper's contact and delivery details.
        var coupon = await _pricing.ValidateCouponAsync(_cart.CouponCode, cart.Lines, ct);

        var totals = _pricing.Calculate(cart.Lines, coupon.Coupon);

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            // Take the stock first. A conditional UPDATE is the whole guard
            // against overselling: two shoppers buying the last item at the
            // same moment both reach this line, and only one of them changes
            // a row. Reading the stock and then writing it would let both
            // through.
            foreach (var line in cart.Lines)
            {
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE ProductVariants
                       SET StockQuantity = StockQuantity - {line.Quantity}
                       WHERE Id = {line.VariantId}
                         AND IsActive = 1
                         AND StockQuantity >= {line.Quantity}",
                    ct);

                if (affected != 1)
                {
                    await transaction.RollbackAsync(ct);

                    _logger.LogInformation(
                        "Order abandoned: {Sku} ran out between the cart and the submit.",
                        line.Sku);

                    return new PlaceOrderResult(
                        null, PlaceOrderProblem.StockGone, line.ProductName);
                }
            }

            var order = BuildOrder(form, cart, totals, coupon.Coupon, culture, userId);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(ct);

            // The order number is built from the identity key, which only
            // exists after the insert. A throwaway value carries the row past
            // the NOT NULL and unique constraints until then.
            order.OrderNumber = $"{order.PlacedUtc.Year}-{order.Id:D6}";
            await _context.SaveChangesAsync(ct);

            if (coupon.Coupon is { } used)
            {
                // Conditional again, so a coupon with a usage limit cannot be
                // spent past it by two simultaneous orders.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"UPDATE Coupons
                       SET TimesUsed = TimesUsed + 1
                       WHERE Id = {used.Id}
                         AND (UsageLimit IS NULL OR TimesUsed < UsageLimit)",
                    ct);
            }

            var provider = _payments.FirstOrDefault(p => p.Method == form.PaymentMethod)
                ?? _payments.First(p => p.Method == PaymentMethod.CashOnDelivery);

            var payment = await provider.AuthoriseAsync(order, ct);

            if (!payment.Succeeded)
            {
                await transaction.RollbackAsync(ct);
                return new PlaceOrderResult(null, PlaceOrderProblem.PaymentDeclined);
            }

            order.PaymentStatus = payment.Status;
            order.PaymentReference = payment.Reference;
            await _context.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            _cart.Clear();
            RememberPlaced(order.OrderNumber);

            // Deliberately after the commit: a mail server having a bad day
            // must not undo an order that is already paid for and stocked.
            await _email.SendOrderPlacedAsync(order, ct);

            return new PlaceOrderResult(order, PlaceOrderProblem.None);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    private Order BuildOrder(
        CheckoutFormViewModel form,
        ViewModels.Cart.CartViewModel cart,
        ViewModels.Cart.PriceBreakdownViewModel totals,
        Coupon? coupon,
        string culture,
        string? userId)
    {
        var order = new Order
        {
            // Replaced with the real number once the row has an id.
            OrderNumber = Guid.NewGuid().ToString("N")[..16],
            UserId = userId,
            Status = OrderStatus.New,
            PlacedUtc = DateTime.UtcNow,
            Culture = culture,

            Email = form.Email.Trim(),
            FirstName = form.FirstName.Trim(),
            LastName = form.LastName.Trim(),
            PhoneNumber = form.PhoneNumber.Trim(),

            ShippingStreet = form.Street.Trim(),
            ShippingStreetNumber = form.StreetNumber?.Trim(),
            ShippingCity = form.City.Trim(),
            ShippingPostalCode = form.PostalCode.Trim(),
            ShippingCountry = "RS",

            CompanyName = form.WantsInvoice ? form.CompanyName?.Trim() : null,
            CompanyTaxNumber = form.WantsInvoice ? form.CompanyTaxNumber?.Trim() : null,

            Subtotal = totals.Subtotal,
            DiscountAmount = totals.DiscountAmount,
            ShippingFee = totals.ShippingFee,
            VatAmount = totals.VatAmount,
            VatRate = totals.VatRate,
            Total = totals.Total,
            Currency = totals.Currency,

            CouponId = coupon?.Id,
            PaymentMethod = form.PaymentMethod,
            PaymentStatus = PaymentStatus.Pending,
            CourierName = form.Courier,
            CustomerNote = form.Note?.Trim()
        };

        if (form.DifferentBillingAddress)
        {
            order.BillingStreet = form.BillingStreet?.Trim();
            order.BillingCity = form.BillingCity?.Trim();
            order.BillingPostalCode = form.BillingPostalCode?.Trim();
        }

        foreach (var line in cart.Lines)
        {
            // Names, codes and prices are copied rather than referenced. If a
            // product is renamed, repriced or withdrawn tomorrow, this order
            // still reads as what was actually bought today.
            order.Items.Add(new OrderItem
            {
                ProductVariantId = line.VariantId,
                ProductName = line.ProductName,
                Sku = line.Sku,
                VariantDescription = line.VariantDescription,
                ImagePath = line.ImagePath,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity
            });
        }

        return order;
    }

    public async Task<Order?> FindOwnedAsync(string orderNumber, string userId, CancellationToken ct) =>
        await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == userId, ct);

    public async Task<Order?> FindForGuestAsync(string orderNumber, string email, CancellationToken ct)
    {
        var number = orderNumber.Trim();
        var address = email.Trim();

        // The email comparison is case-insensitive by the column collation.
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == number && o.Email == address, ct);
    }

    public async Task<Order?> FindPlacedThisSessionAsync(string orderNumber, CancellationToken ct)
    {
        if (!PlacedThisSession().Contains(orderNumber))
        {
            return null;
        }

        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<List<Order>> ListForUserAsync(string userId, CancellationToken ct) =>
        await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.PlacedUtc)
            .ToListAsync(ct);

    // Earlier versions let anyone holding a number see a guest order. Order
    // numbers are sequential, so walking 2026-000001, 000002 ... would have
    // listed every guest's name, address and phone. Access now needs one of:
    // ownership, the number plus the email, or this session having placed it.

    private HashSet<string> PlacedThisSession()
    {
        var raw = _http.HttpContext?.Session.GetString(PlacedKey);

        if (string.IsNullOrEmpty(raw))
        {
            return [];
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(raw) ?? [];
        }
        catch (System.Text.Json.JsonException)
        {
            return [];
        }
    }

    private void RememberPlaced(string orderNumber)
    {
        var session = _http.HttpContext?.Session;

        if (session is null)
        {
            return;
        }

        var placed = PlacedThisSession();
        placed.Add(orderNumber);

        // A handful is plenty; nobody places dozens of orders in one sitting.
        if (placed.Count > 10)
        {
            placed = [.. placed.TakeLast(10)];
        }

        session.SetString(PlacedKey, System.Text.Json.JsonSerializer.Serialize(placed));
    }
}
