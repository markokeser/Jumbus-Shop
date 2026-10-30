using System.Globalization;

namespace WebShop.Infrastructure;

/// <summary>
/// Everything is stored in UTC; shoppers read Belgrade time. The IANA id works
/// on Linux and on Windows with ICU (.NET 6+); the Windows id is the fallback
/// for older Windows setups. UTC is the last resort rather than an exception.
/// </summary>
public static class ShopTime
{
    private static readonly TimeZoneInfo Zone = Resolve();

    private static TimeZoneInfo Resolve()
    {
        foreach (var id in new[] { "Europe/Belgrade", "Central Europe Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    /// <summary>MySQL hands dates back with an unspecified kind; they were written as UTC.</summary>
    public static DateTime Local(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime? Local(DateTime? utc) => utc is { } value ? Local(value) : null;

    /// <summary>
    /// Admin forms take Belgrade time; the database stores UTC. A time that
    /// does not exist — the hour skipped when clocks go forward — is moved on
    /// by an hour instead of throwing.
    /// </summary>
    public static DateTime ToUtc(DateTime local)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);

        if (Zone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, Zone);
    }

    public static DateTime? ToUtc(DateTime? local) => local is { } value ? ToUtc(value) : null;

    public static string Date(DateTime utc) =>
        Local(utc).ToString("d", CultureInfo.CurrentCulture);

    public static string DateAndTime(DateTime utc) =>
        Local(utc).ToString("g", CultureInfo.CurrentCulture);
}
