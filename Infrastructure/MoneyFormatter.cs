using System.Globalization;

namespace WebShop.Infrastructure;

/// <summary>
/// Formats amounts consistently across views. The currency code is written
/// after the number rather than using CultureInfo's own currency symbol,
/// because the English page still charges dinars — a customer reading the
/// English site is buying in RSD, not USD.
/// </summary>
public static class MoneyFormatter
{
    public static string Format(decimal amount, string currency = "RSD") =>
        $"{amount.ToString("N0", CultureInfo.CurrentCulture)} {currency}";

    public static string FormatWithDecimals(decimal amount, string currency = "RSD") =>
        $"{amount.ToString("N2", CultureInfo.CurrentCulture)} {currency}";
}
