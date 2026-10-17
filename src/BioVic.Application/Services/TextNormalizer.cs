using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BioVic.Application.Services;

public static partial class TextNormalizer
{
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var decomposed = value.Trim().ToUpperInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.IsLetterOrDigit(character) ? character : ' ');
            }
        }

        return MultipleWhitespaceRegex().Replace(builder.ToString().Normalize(NormalizationForm.FormC), " ").Trim();
    }

    public static string JoinNormalized(params string?[] values) =>
        Normalize(string.Join(' ', values.Where(value => !string.IsNullOrWhiteSpace(value))));

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultipleWhitespaceRegex();
}
