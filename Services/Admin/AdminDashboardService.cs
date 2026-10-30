using Microsoft.EntityFrameworkCore;
using WebShop.Data;
using WebShop.Infrastructure;
using WebShop.Models.Catalog;
using WebShop.Models.Orders;
using WebShop.ViewModels.Admin;

namespace WebShop.Services.Admin;

public interface IAdminDashboardService
{
    Task<DashboardViewModel> GetAsync(string culture, CancellationToken ct);

    Task<AdminNavCounts> NavCountsAsync(CancellationToken ct);
}

public class AdminDashboardService : IAdminDashboardService
{
    private const int Days = 30;

    private readonly ApplicationDbContext _context;

    public AdminDashboardService(ApplicationDbContext context) => _context = context;

    public async Task<DashboardViewModel> GetAsync(string culture, CancellationToken ct)
    {
        var model = new DashboardViewModel();

        // Days are Belgrade days: an order at 00:30 local belongs to that
        // morning, not to the previous UTC evening.
        var today = DateOnly.FromDateTime(ShopTime.Local(DateTime.UtcNow));
        var firstDay = today.AddDays(-(Days - 1));
        var sinceUtc = ShopTime.ToUtc(firstDay.ToDateTime(TimeOnly.MinValue));

        // Cancelled and returned orders are not revenue.
        var placed = await _context.Orders
            .AsNoTracking()
            .Where(o => o.PlacedUtc >= sinceUtc
                && o.Status != OrderStatus.Cancelled
                && o.Status != OrderStatus.Returned)
            .Select(o => new { o.PlacedUtc, o.Total })
            .ToListAsync(ct);

        var byDay = placed
            .GroupBy(o => DateOnly.FromDateTime(ShopTime.Local(o.PlacedUtc)))
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(o => o.Total), Orders: g.Count()));

        model.Daily = [.. Enumerable.Range(0, Days)
            .Select(i => firstDay.AddDays(i))
            .Select(day => byDay.TryGetValue(day, out var d)
                ? new DailyRevenue(day, d.Revenue, d.Orders)
                : new DailyRevenue(day, 0, 0))];

        model.Revenue30 = placed.Sum(o => o.Total);
        model.Orders30 = placed.Count;
        model.OrdersToday = byDay.TryGetValue(today, out var t) ? t.Orders : 0;

        model.NewOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.New, ct);
        model.ToShip = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Confirmed, ct);
        model.PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved, ct);
        model.LowStockCount = await LowStock().CountAsync(ct);

        model.LowStock = await AdminStockService.ToRowsAsync(
            _context, LowStock().OrderBy(v => v.StockQuantity).ThenBy(v => v.Id).Take(8), culture, ct);

        model.RecentOrders = await AdminOrderService.ToRows(
                _context.Orders.AsNoTracking().OrderByDescending(o => o.PlacedUtc).Take(10))
            .ToListAsync(ct);

        return model;
    }

    public async Task<AdminNavCounts> NavCountsAsync(CancellationToken ct) => new()
    {
        NewOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.New, ct),
        PendingReviews = await _context.Reviews.CountAsync(r => !r.IsApproved, ct),
        LowStock = await LowStock().CountAsync(ct)
    };

    /// <summary>On sale and at or under its reorder point. Archived products do not count.</summary>
    private IQueryable<ProductVariant> LowStock() =>
        _context.ProductVariants
            .AsNoTracking()
            .Where(v => v.IsActive
                && v.Product.Status != ProductStatus.Archived
                && v.StockQuantity <= v.LowStockThreshold);
}
