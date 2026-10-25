using Cofoundry.Core;

namespace Cofoundry.Web.Admin.Blazor.Components.Images;

/// <summary>
/// Restrictions an [Image] or [ImageCollection] property puts on the images it accepts. The image
/// picker opens with them applied and doesn't let them be changed, as the AngularJS picker did.
/// </summary>
public sealed record ImageAssetFilter
{
    public static readonly ImageAssetFilter None = new();

    /// <summary>
    /// Tags in the format <see cref="TagParser"/> reads: comma separated, quoted if they contain a space.
    /// </summary>
    public string? Tags { get; init; }

    public int? Width { get; init; }

    public int? Height { get; init; }

    public int? MinWidth { get; init; }

    public int? MinHeight { get; init; }

    /// <summary>
    /// Describes the size restrictions, e.g. "min-size 400x300", for the upload dialog's warning.
    /// Empty when there are none.
    /// </summary>
    public string DescribeSize()
    {
        var parts = new List<string>();
        AddSize(Width, Height, string.Empty);
        AddSize(MinWidth, MinHeight, "min-");

        return string.Join(", ", parts);

        void AddSize(int? width, int? height, string prefix)
        {
            if (width > 0 && height > 0)
            {
                parts.Add($"{prefix}size {width}x{height}");
                return;
            }

            if (width > 0)
            {
                parts.Add($"{prefix}width {width}");
            }

            if (height > 0)
            {
                parts.Add($"{prefix}height {height}");
            }
        }
    }

    /// <summary>
    /// True if an image of this size meets the size restrictions. As the AngularJS image upload field, a
    /// restriction of 0 or 1 is ignored.
    /// </summary>
    public bool IsSizeMet(int width, int height)
    {
        return IsMinSize(MinWidth, width)
            && IsMinSize(MinHeight, height)
            && IsSize(Width, width)
            && IsSize(Height, height);

        static bool IsMinSize(int? size, int value) => !(size > 1) || value >= size;

        static bool IsSize(int? size, int value) => !(size > 1) || value == size;
    }

    /// <summary>
    /// Joins tags into the format <see cref="Tags"/> uses.
    /// </summary>
    public static string? JoinTags(IEnumerable<string>? tags)
    {
        var formatted = tags?
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Contains(' ') ? $"\"{t}\"" : t)
            .ToList();

        return formatted?.Count > 0 ? string.Join(",", formatted) : null;
    }

    /// <summary>
    /// Splits <see cref="Tags"/> back into tags, e.g. to tag an upload with them.
    /// </summary>
    public static IReadOnlyCollection<string> SplitTags(string? tags)
    {
        return (tags ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => t.Trim('"'))
            .ToList();
    }
}
