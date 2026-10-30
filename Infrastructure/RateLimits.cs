namespace WebShop.Infrastructure;

/// <summary>
/// Named rate-limit policies. "Sensitive" covers forms an attacker would want
/// to hammer: sign-in, registration, password reset and guest order lookup.
/// Ten a minute per address is invisible to a real person and useless to a script.
///
/// Behind a reverse proxy every request arrives from the proxy's address, so
/// forwarded headers have to be configured before this limits anything useful.
/// </summary>
public static class RateLimits
{
    public const string Sensitive = "sensitive";
}
