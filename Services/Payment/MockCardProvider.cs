using WebShop.Models.Orders;

namespace WebShop.Services.Payment;

/// <summary>
/// Stand-in for a card gateway. It approves everything and invents a
/// reference, which is enough to exercise the checkout end to end.
///
/// A real integration is not a matter of filling in this class. Domestic
/// processors (NestPay through a bank, WSPay, Monri) redirect the shopper to
/// the bank's own hosted form, then call back to a URL on this site with a
/// signed payload that has to be verified before the order is treated as
/// paid. That means two extra endpoints and a signing secret, which is why
/// it belongs in its own piece of work rather than here.
///
/// Until then: never let this run outside Development.
/// </summary>
public class MockCardProvider : IPaymentProvider
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<MockCardProvider> _logger;

    public MockCardProvider(IWebHostEnvironment environment, ILogger<MockCardProvider> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public PaymentMethod Method => PaymentMethod.Card;

    public Task<PaymentResult> AuthoriseAsync(Order order, CancellationToken ct)
    {
        if (!_environment.IsDevelopment())
        {
            _logger.LogError(
                "Card payment was attempted on order {Order} with no gateway configured.",
                order.OrderNumber);

            return Task.FromResult(new PaymentResult(
                false, PaymentStatus.Failed, Error: "No payment gateway is configured."));
        }

        var reference = "MOCK-" + Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        _logger.LogWarning(
            "Mock card payment approved for order {Order}, reference {Reference}. " +
            "No money has moved.",
            order.OrderNumber, reference);

        return Task.FromResult(new PaymentResult(true, PaymentStatus.Paid, reference));
    }
}
