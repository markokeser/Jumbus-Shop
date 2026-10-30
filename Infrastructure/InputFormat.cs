using System.Globalization;

namespace WebShop.Infrastructure;

/// <summary>
/// Values for HTML inputs, which take one fixed format whatever the page's
/// language. A Serbian-formatted "6990,50" in a number field shows as empty,
/// and the price is lost on the next save.
/// </summary>
public static class InputFormat
{
    public static string Number(decimal? value) =>
        value?.ToString("0.##", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string DateTimeLocal(DateTime? value) =>
        value?.ToString("yyyy-MM-dd'T'HH:mm", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string Date(DateTime? value) =>
        value?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;
}
