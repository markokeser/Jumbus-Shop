using WebShop.Models.Orders;

namespace WebShop.Services.Email;

public interface IOrderEmailSender
{
    Task SendOrderPlacedAsync(Order order, CancellationToken ct);

    Task SendStatusChangedAsync(Order order, OrderStatus previous, CancellationToken ct);
}

/// <summary>
/// Writes what would have been sent to the log. Swap this for a real sender
/// (SMTP, or a transactional provider) by registering a different
/// implementation in Program.cs — nothing else has to change.
///
/// The order confirmation is the one message a shop legally has to send, so
/// this is a placeholder with a deadline rather than an optional extra.
/// </summary>
public class LoggingOrderEmailSender : IOrderEmailSender
{
    private readonly ILogger<LoggingOrderEmailSender> _logger;

    public LoggingOrderEmailSender(ILogger<LoggingOrderEmailSender> logger) => _logger = logger;

    public Task SendOrderPlacedAsync(Order order, CancellationToken ct)
    {
        _logger.LogInformation(
            "EMAIL (not sent) to {Email}: order {Number} confirmed, {Count} items, total {Total} {Currency}.",
            order.Email, order.OrderNumber, order.Items.Count, order.Total, order.Currency);

        return Task.CompletedTask;
    }

    public Task SendStatusChangedAsync(Order order, OrderStatus previous, CancellationToken ct)
    {
        _logger.LogInformation(
            "EMAIL (not sent) to {Email}: order {Number} moved from {Previous} to {Status}.",
            order.Email, order.OrderNumber, previous, order.Status);

        return Task.CompletedTask;
    }
}
