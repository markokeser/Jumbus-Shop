namespace WebShop.Services.Email;

public interface IAccountEmailSender
{
    Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct);

    Task SendWelcomeAsync(string email, string? firstName, CancellationToken ct);
}

/// <summary>
/// Writes to the log instead of sending. The reset link is logged only in
/// Development: a password-reset token sitting in production logs is as good
/// as the password to anyone who can read them.
/// </summary>
public class LoggingAccountEmailSender : IAccountEmailSender
{
    private readonly ILogger<LoggingAccountEmailSender> _logger;
    private readonly IWebHostEnvironment _environment;

    public LoggingAccountEmailSender(
        ILogger<LoggingAccountEmailSender> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken ct)
    {
        if (_environment.IsDevelopment())
        {
            _logger.LogInformation("EMAIL (not sent) to {Email}: reset your password at {Url}", email, resetUrl);
        }
        else
        {
            _logger.LogWarning(
                "Password reset requested for {Email}, but no mail sender is configured. The link was not delivered.",
                email);
        }

        return Task.CompletedTask;
    }

    public Task SendWelcomeAsync(string email, string? firstName, CancellationToken ct)
    {
        _logger.LogInformation("EMAIL (not sent) to {Email}: welcome, {Name}.", email, firstName ?? email);
        return Task.CompletedTask;
    }
}
