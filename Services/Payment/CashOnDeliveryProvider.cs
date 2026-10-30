using WebShop.Models.Orders;

namespace WebShop.Services.Payment;

/// <summary>
/// Cash on delivery. Nothing is taken now — the courier collects, and the
/// order is marked paid by hand in the admin panel once the money arrives.
/// This is still the most common way to pay online in Serbia, so it is the
/// default rather than an afterthought.
/// </summary>
public class CashOnDeliveryProvider : IPaymentProvider
{
    public PaymentMethod Method => PaymentMethod.CashOnDelivery;

    public Task<PaymentResult> AuthoriseAsync(Order order, CancellationToken ct) =>
        Task.FromResult(new PaymentResult(true, PaymentStatus.Pending));
}
