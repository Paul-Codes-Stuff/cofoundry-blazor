using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Cofoundry.Web.Admin.Blazor;

public static partial class StringUtilities
{
    /// <summary>
    /// Port of the AngularJS <c>stringUtilities.slugify</c>: strips diacritics, turns separators into
    /// hyphens, removes other punctuation and lower-cases. Used to suggest a url path from a title.
    /// </summary>
    public static string? Slugify(string? value, int limitTo = int.MaxValue)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var result = RemoveDiacritics(value)
            .Replace("&", " and ");

        result = SeparatorRegex().Replace(result, " ");
        result = NonWordRegex().Replace(result, string.Empty)
            .ToLowerInvariant()
            .Trim();
        result = WhitespaceRegex().Replace(result, "-");
        result = HyphensRegex().Replace(result, "-");

        if (result.StartsWith('-'))
        {
            result = result[1..];
        }

        // The AngularJS version truncated the input rather than the result (a bug); truncating
        // the result is what the limit is clearly meant to do.
        if (result.Length > limitTo)
        {
            result = result[..limitTo].TrimEnd('-');
        }

        return result;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[/\\\.,\+=–—:_]")]
    private static partial Regex SeparatorRegex();

    [GeneratedRegex(@"[^\w\s-]")]
    private static partial Regex NonWordRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("-+")]
    private static partial Regex HyphensRegex();
}
