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

    /// <summary>
    /// Formats a file size as e.g. "1.5 mb", matching the AngularJS admin's "bytes" filter.
    /// </summary>
    public static string FormatBytes(long bytes)
    {
        string[] units = ["bytes", "kb", "mb", "gb", "tb", "pb"];

        if (bytes <= 0)
        {
            return "0 " + units[1];
        }

        var number = Math.Min((int)Math.Floor(Math.Log(bytes) / Math.Log(1024)), units.Length - 1);

        return (bytes / Math.Pow(1024, number)).ToString("0.0", CultureInfo.InvariantCulture) + " " + units[number];
    }

    /// <summary>
    /// "my-photo.jpg" becomes "My-photo", used to default an asset title from its file name.
    /// </summary>
    public static string TitleFromFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);

        if (string.IsNullOrEmpty(name))
        {
            name = fileName;
        }

        return name.Length == 0 ? name : char.ToUpper(name[0], CultureInfo.CurrentCulture) + name[1..];
    }

    /// <summary>
    /// "TopLeft" becomes "Top Left", for showing enum values as labels.
    /// </summary>
    public static string SplitPascalCase(string value) => PascalCaseBoundaryRegex().Replace(value, " ");

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

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex PascalCaseBoundaryRegex();

    [GeneratedRegex("-+")]
    private static partial Regex HyphensRegex();
}
