namespace WebShop.Infrastructure;

/// <summary>
/// Turns what a shopper typed into SQL LIKE patterns.
///
/// The database columns are utf8mb4_unicode_ci, which already folds č, ć, š
/// and ž onto c, s and z — so "ciscenje" finds "čišćenje" with no help from
/// us. Đ is the exception: Unicode treats it as its own letter, so it folds
/// to neither "d" nor "dj". That one case is covered by searching for a
/// second, rewritten term alongside the original.
/// </summary>
public static class SearchTerms
{
    public const int MinimumLength = 2;
    private const int MaximumLength = 80;

    /// <summary>
    /// Returns one or two LIKE patterns. Two whenever the query could be
    /// written with or without đ; callers OR them together.
    /// </summary>
    public static IReadOnlyList<string> Build(string? query)
    {
        var trimmed = query?.Trim();

        if (string.IsNullOrEmpty(trimmed) || trimmed.Length < MinimumLength)
        {
            return [];
        }

        if (trimmed.Length > MaximumLength)
        {
            trimmed = trimmed[..MaximumLength];
        }

        var primary = Escape(trimmed);
        var terms = new List<string> { $"%{primary}%" };

        // Someone on an English keyboard types "dj"; someone on a Serbian one
        // types "đ". Both should find the same product.
        var alternate = trimmed.Contains("dj", StringComparison.OrdinalIgnoreCase)
            ? ReplaceIgnoreCase(trimmed, "dj", "đ")
            : trimmed.Contains('đ') || trimmed.Contains('Đ')
                ? trimmed.Replace("đ", "dj").Replace("Đ", "Dj")
                : null;

        if (alternate is not null)
        {
            terms.Add($"%{Escape(alternate)}%");
        }

        return terms;
    }

    /// <summary>
    /// Neutralises the LIKE wildcards so a query of "100%" searches for the
    /// text rather than matching every row.
    /// </summary>
    private static string Escape(string value) => value
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");

    private static string ReplaceIgnoreCase(string source, string find, string replace)
    {
        var result = source;
        var index = result.IndexOf(find, StringComparison.OrdinalIgnoreCase);

        while (index >= 0)
        {
            result = result[..index] + replace + result[(index + find.Length)..];
            index = result.IndexOf(find, index + replace.Length, StringComparison.OrdinalIgnoreCase);
        }

        return result;
    }
}
