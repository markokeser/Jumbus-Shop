namespace WebShop.Models.Orders;

public enum OrderStatus
{
    New = 0,
    Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    Returned = 5
}

public enum PaymentMethod
{
    CashOnDelivery = 0,
    Card = 1
}

public enum PaymentStatus
{
    Pending = 0,
    Authorised = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}

public enum AddressType
{
    Shipping = 0,
    Billing = 1
}

public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1,
    FreeShipping = 2
}
