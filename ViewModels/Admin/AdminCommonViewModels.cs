namespace WebShop.ViewModels.Admin;

public class AdminPage
{
    public int Number { get; set; } = 1;

    public int Size { get; set; } = 25;

    public int Total { get; set; }

    public int Count => Math.Max(1, (int)Math.Ceiling(Total / (double)Size));

    public int Skip => (Math.Clamp(Number, 1, Count) - 1) * Size;

    public static AdminPage Of(int requested, int size, int total) =>
        new() { Number = Math.Clamp(requested, 1, Math.Max(1, (int)Math.Ceiling(total / (double)size))), Size = size, Total = total };
}

/// <summary>An option for a select list, without dragging MVC's SelectListItem into services.</summary>
public record AdminOption(string Value, string Text);

public class AdminNavCounts
{
    public int NewOrders { get; set; }

    public int PendingReviews { get; set; }

    public int LowStock { get; set; }
}

public class DashboardViewModel
{
    public decimal Revenue30 { get; set; }

    public int Orders30 { get; set; }

    public decimal AverageOrder30 => Orders30 == 0 ? 0 : Math.Round(Revenue30 / Orders30, 0);

    public int OrdersToday { get; set; }

    public int NewOrders { get; set; }

    public int ToShip { get; set; }

    public int PendingReviews { get; set; }

    public int LowStockCount { get; set; }

    public List<DailyRevenue> Daily { get; set; } = [];

    public List<StockRow> LowStock { get; set; } = [];

    public List<AdminOrderRow> RecentOrders { get; set; } = [];

    public decimal DailyMax => Daily.Count == 0 ? 0 : Daily.Max(d => d.Revenue);
}

public record DailyRevenue(DateOnly Day, decimal Revenue, int Orders);
