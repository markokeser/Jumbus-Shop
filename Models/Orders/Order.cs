using WebShop.Models.Identity;

namespace WebShop.Models.Orders;

public class Order
{
    public int Id { get; set; }

    /// <summary>Human-facing number shown to the customer, e.g. "2026-000148".</summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>Null for guest checkout.</summary>
    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.New;

    public DateTime PlacedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ConfirmedUtc { get; set; }

    public DateTime? ShippedUtc { get; set; }

    public DateTime? DeliveredUtc { get; set; }

    /// <summary>Language the order was placed in, so confirmation emails match.</summary>
    public string Culture { get; set; } = "sr";

    // Contact details, copied at checkout rather than referenced.
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    // Shipping address snapshot.
    public string ShippingStreet { get; set; } = string.Empty;

    public string? ShippingStreetNumber { get; set; }

    public string ShippingCity { get; set; } = string.Empty;

    public string ShippingPostalCode { get; set; } = string.Empty;

    public string ShippingCountry { get; set; } = "RS";

    /// <summary>Set only when the customer asks for a different billing address.</summary>
    public string? BillingStreet { get; set; }

    public string? BillingCity { get; set; }

    public string? BillingPostalCode { get; set; }

    /// <summary>Company name and tax number, for customers who need an invoice.</summary>
    public string? CompanyName { get; set; }

    public string? CompanyTaxNumber { get; set; }

    // Money. Every figure is stored, never recalculated on read, because
    // prices and VAT rates change and old orders must stay as they were.
    public decimal Subtotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal ShippingFee { get; set; }

    /// <summary>VAT already contained in Total, shown separately on the invoice.</summary>
    public decimal VatAmount { get; set; }

    public decimal Total { get; set; }

    public decimal VatRate { get; set; }

    public string Currency { get; set; } = "RSD";

    public int? CouponId { get; set; }

    public Coupon? Coupon { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>Reference returned by the payment provider, null for cash on delivery.</summary>
    public string? PaymentReference { get; set; }

    public string? CourierName { get; set; }

    public string? TrackingNumber { get; set; }

    public string? CustomerNote { get; set; }

    /// <summary>Internal note, never shown to the customer.</summary>
    public string? AdminNote { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
