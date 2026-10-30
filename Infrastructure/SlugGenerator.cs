using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WebShop.Infrastructure;

/// <summary>
/// Turns a product or category name into a URL-safe slug.
/// Handles Serbian Latin diacritics explicitly, because Unicode
/// normalisation alone maps "đ" to nothing useful.
/// </summary>
public static partial class SlugGenerator
{
    private static readonly Dictionary<char, string> SerbianMap = new()
    {
        ['č'] = "c", ['ć'] = "c", ['ž'] = "z", ['š'] = "s", ['đ'] = "dj",
        ['Č'] = "c", ['Ć'] = "c", ['Ž'] = "z", ['Š'] = "s", ['Đ'] = "dj"
    };

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidCharacters();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex Separators();

    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(input.Length);
        foreach (var character in input)
        {
            if (SerbianMap.TryGetValue(character, out var replacement))
            {
                builder.Append(replacement);
            }
            else
            {
                builder.Append(character);
            }
        }

        var normalised = builder.ToString()
            .Normalize(NormalizationForm.FormD)
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
            .ToString()
            .ToLowerInvariant();

        normalised = InvalidCharacters().Replace(normalised, string.Empty);
        normalised = Separators().Replace(normalised, "-");

        return normalised.Trim('-');
    }
}
