using System.ComponentModel.DataAnnotations;
using WebShop.Models.Orders;

namespace WebShop.ViewModels.Admin;

public class AdminOrderQuery
{
    public OrderStatus? Status { get; set; }

    public string? Q { get; set; }

    public int Page { get; set; } = 1;
}

public class AdminOrderRow
{
    public string OrderNumber { get; set; } = string.Empty;

    public DateTime PlacedUtc { get; set; }

    public string Customer { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public int ItemCount { get; set; }

    public decimal Total { get; set; }

    public string Currency { get; set; } = "RSD";

    public OrderStatus Status { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public PaymentStatus PaymentStatus { get; set; }

    public bool IsGuest { get; set; }
}

public class AdminOrderListViewModel
{
    public AdminOrderQuery Query { get; set; } = new();

    public List<AdminOrderRow> Orders { get; set; } = [];

    public AdminPage Page { get; set; } = new();

    /// <summary>How many orders sit in each status, for the tabs.</summary>
    public Dictionary<OrderStatus, int> Counts { get; set; } = [];

    public int AllCount => Counts.Values.Sum();
}

public class AdminOrderDetailsViewModel
{
    public Order Order { get; set; } = null!;

    public string? CouponCode { get; set; }

    public int CustomerOrderCount { get; set; }

    public IReadOnlyList<OrderStatus> NextSteps { get; set; } = [];

    public StatusChangeForm Change { get; set; } = new();
}

public class StatusChangeForm
{
    public OrderStatus Target { get; set; }

    [StringLength(120)]
    public string? Courier { get; set; }

    [StringLength(120)]
    public string? TrackingNumber { get; set; }

    /// <summary>For returns: put the goods back on sale. Cancelled orders always restock.</summary>
    public bool Restock { get; set; } = true;
}

public enum StatusChangeOutcome
{
    Changed = 0,
    NotFound = 1,
    NotAllowed = 2,
    TrackingRequired = 3,

    /// <summary>Someone else moved the order first; nothing was changed.</summary>
    Conflict = 4
}
