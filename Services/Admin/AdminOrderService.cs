using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Orders;
using WebShop.Services.Email;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public interface IAdminOrderService
{
    Task<AdminOrderListViewModel> ListAsync(AdminOrderQuery query, CancellationToken ct);

    Task<AdminOrderDetailsViewModel?> DetailsAsync(string orderNumber, CancellationToken ct);

    Task<StatusChangeOutcome> ChangeStatusAsync(string orderNumber, StatusChangeForm change, CancellationToken ct);

    Task<bool> SetPaymentStatusAsync(string orderNumber, PaymentStatus status, CancellationToken ct);

    Task<bool> SaveNoteAsync(string orderNumber, string? note, CancellationToken ct);
}

public class AdminOrderService : IAdminOrderService
{
    private const int PageSize = 25;

    private readonly ApplicationDbContext _context;
    private readonly IOrderEmailSender _email;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(ApplicationDbContext context, IOrderEmailSender email, ILogger<AdminOrderService> logger)
    {
        _context = context;
        _email = email;
        _logger = logger;
    }

    public static IQueryable<AdminOrderRow> ToRows(IQueryable<Order> orders) =>
        orders.Select(o => new AdminOrderRow
        {
            OrderNumber = o.OrderNumber,
            PlacedUtc = o.PlacedUtc,
            Customer = o.FirstName + " " + o.LastName,
            Email = o.Email,
            City = o.ShippingCity,
            ItemCount = o.Items.Sum(i => (int?)i.Quantity) ?? 0,
            Total = o.Total,
            Currency = o.Currency,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            PaymentStatus = o.PaymentStatus,
            IsGuest = o.UserId == null
        });

    public async Task<AdminOrderListViewModel> ListAsync(AdminOrderQuery query, CancellationToken ct)
    {
        var orders = _context.Orders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Q))
        {
            var term = query.Q.Trim();
            orders = orders.Where(o => o.OrderNumber.Contains(term)
                || o.Email.Contains(term)
                || o.LastName.Contains(term)
                || o.PhoneNumber.Contains(term));
        }

        var counts = await orders
            .GroupBy(o => o.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(ct);

        if (query.Status is { } status)
        {
            orders = orders.Where(o => o.Status == status);
        }

        var total = await orders.CountAsync(ct);
        var page = AdminPage.Of(query.Page, PageSize, total);

        return new AdminOrderListViewModel
        {
            Query = query,
            Page = page,
            Counts = counts.ToDictionary(c => c.Key, c => c.Count),
            Orders = await ToRows(orders
                    .OrderByDescending(o => o.PlacedUtc)
                    .ThenByDescending(o => o.Id)
                    .Skip(page.Skip)
                    .Take(page.Size))
                .ToListAsync(ct)
        };
    }

    public async Task<AdminOrderDetailsViewModel?> DetailsAsync(string orderNumber, CancellationToken ct)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Coupon)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

        if (order is null)
        {
            return null;
        }

        // Returning customers are worth recognising; guests are matched by email.
        var previous = order.UserId is { } userId
            ? await _context.Orders.CountAsync(o => o.UserId == userId, ct)
            : await _context.Orders.CountAsync(o => o.Email == order.Email, ct);

        return new AdminOrderDetailsViewModel
        {
            Order = order,
            CouponCode = order.Coupon?.Code,
            CustomerOrderCount = previous,
            NextSteps = OrderWorkflow.NextSteps(order.Status),
            Change = new StatusChangeForm { Courier = order.CourierName, TrackingNumber = order.TrackingNumber }
        };
    }

    /// <summary>
    /// Moves an order one step, with everything that has to happen alongside:
    /// timestamps, tracking details, stock back on sale for a cancellation or
    /// a return, the coupon use given back for a cancellation, and cash on
    /// delivery marked paid once the courier has delivered.
    ///
    /// The status is claimed first with a conditional UPDATE. Two admins
    /// pressing "Cancel" at the same moment would otherwise both restock the
    /// goods; here the second one matches no row and changes nothing.
    /// </summary>
    public async Task<StatusChangeOutcome> ChangeStatusAsync(
        string orderNumber,
        StatusChangeForm change,
        CancellationToken ct)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

        if (order is null)
        {
            return StatusChangeOutcome.NotFound;
        }

        var previous = order.Status;

        if (!OrderWorkflow.NextSteps(previous).Contains(change.Target))
        {
            return StatusChangeOutcome.NotAllowed;
        }

        var tracking = AdminText.Clean(change.TrackingNumber);
        var courier = AdminText.Clean(change.Courier);

        if (change.Target == OrderStatus.Shipped)
        {
            if (tracking is null)
            {
                return StatusChangeOutcome.TrackingRequired;
            }

            if (courier is null || !Couriers.All.Contains(courier))
            {
                courier = order.CourierName;
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var claimed = await _context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Orders SET Status = {(int)change.Target} WHERE Id = {order.Id} AND Status = {(int)previous}",
            ct);

        if (claimed != 1)
        {
            await transaction.RollbackAsync(ct);
            return StatusChangeOutcome.Conflict;
        }

        var now = DateTime.UtcNow;
        order.Status = change.Target;

        switch (change.Target)
        {
            case OrderStatus.Confirmed:
                order.ConfirmedUtc ??= now;
                break;

            case OrderStatus.Shipped:
                order.ConfirmedUtc ??= now;
                order.ShippedUtc = now;
                order.CourierName = courier;
                order.TrackingNumber = tracking;
                break;

            case OrderStatus.Delivered:
                order.DeliveredUtc = now;

                // The courier collected the money at the door.
                if (order.PaymentMethod == PaymentMethod.CashOnDelivery && order.PaymentStatus == PaymentStatus.Pending)
                {
                    order.PaymentStatus = PaymentStatus.Paid;
                }
                break;

            case OrderStatus.Cancelled:
                // The goods never left, so they always go back on sale.
                await RestockAsync(order, ct);

                if (order.CouponId is { } couponId)
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync(
                        $"UPDATE Coupons SET TimesUsed = TimesUsed - 1 WHERE Id = {couponId} AND TimesUsed > 0",
                        ct);
                }
                break;

            case OrderStatus.Returned:
                // Returned goods are restocked only if they can be sold again;
                // opened cosmetics usually cannot.
                if (change.Restock)
                {
                    await RestockAsync(order, ct);
                }
                break;
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        try
        {
            await _email.SendStatusChangedAsync(order, previous, ct);
        }
        catch (Exception ex)
        {
            // The change is committed; a mail problem must not undo it or hide it.
            _logger.LogError(ex, "Status mail for order {Number} failed.", order.OrderNumber);
        }

        return StatusChangeOutcome.Changed;
    }

    public async Task<bool> SetPaymentStatusAsync(string orderNumber, PaymentStatus status, CancellationToken ct)
    {
        if (!Enum.IsDefined(status))
        {
            return false;
        }

        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

        if (order is null)
        {
            return false;
        }

        order.PaymentStatus = status;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> SaveNoteAsync(string orderNumber, string? note, CancellationToken ct)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

        if (order is null)
        {
            return false;
        }

        var clean = AdminText.Clean(note);
        order.AdminNote = clean is { Length: > 2000 } ? clean[..2000] : clean;
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private async Task RestockAsync(Order order, CancellationToken ct)
    {
        // Lines whose variant was deleted since have nothing to go back to.
        foreach (var item in order.Items.Where(i => i.ProductVariantId is not null))
        {
            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE ProductVariants SET StockQuantity = StockQuantity + {item.Quantity} WHERE Id = {item.ProductVariantId}",
                ct);
        }
    }
}
