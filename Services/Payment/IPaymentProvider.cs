using WebShop.Models.Orders;

namespace WebShop.Services.Payment;

public record PaymentResult(bool Succeeded, PaymentStatus Status, string? Reference = null, string? Error = null);

/// <summary>
/// One way of taking money. Registered once per method; OrderService picks
/// the provider whose Method matches what the shopper chose.
/// </summary>
public interface IPaymentProvider
{
    PaymentMethod Method { get; }

    Task<PaymentResult> AuthoriseAsync(Order order, CancellationToken ct);
}
