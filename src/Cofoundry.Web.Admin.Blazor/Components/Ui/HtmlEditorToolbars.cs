using System.Text.Json;

namespace Cofoundry.Web.Admin.Blazor.Components.Ui;

/// <summary>
/// Builds a TinyMCE toolbar from [Html] toolbar presets, as the AngularJS cms-form-field-html did.
/// </summary>
public static class HtmlEditorToolbars
{
    private static readonly Dictionary<string, string> Presets = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(HtmlToolbarPreset.Headings)] = "formatselect",
        [nameof(HtmlToolbarPreset.BasicFormatting)] = "fullscreen undo redo removeformat | bold italic underline | link unlink",
        [nameof(HtmlToolbarPreset.AdvancedFormatting)] = "bullist numlist blockquote | alignleft aligncenter alignright alignjustify | strikethrough superscript subscript",
        [nameof(HtmlToolbarPreset.Media)] = "cfimage media",
        [nameof(HtmlToolbarPreset.Source)] = "code"
    };

    private static readonly string[] DefaultPresets = [nameof(HtmlToolbarPreset.Headings), nameof(HtmlToolbarPreset.BasicFormatting)];

    /// <param name="presets">Preset names, e.g. "BasicFormatting". Headings and basic formatting when empty.</param>
    /// <param name="customToolbar">
    /// Used by the "Custom" preset: one toolbar, or several each in quotes, e.g. <c>'bold italic', 'link'</c>.
    /// </param>
    public static string Parse(IEnumerable<string>? presets, string? customToolbar)
    {
        var names = presets?.ToList() is { Count: > 0 } list ? list : [.. DefaultPresets];
        var toolbars = new List<string>();

        foreach (var name in names.Select(n => n.Trim()))
        {
            if (string.Equals(name, nameof(HtmlToolbarPreset.Custom), StringComparison.OrdinalIgnoreCase))
            {
                toolbars.AddRange(ParseCustom(customToolbar).Except(toolbars));
            }
            else if (Presets.TryGetValue(name, out var toolbar))
            {
                toolbars.Add(toolbar);
            }
        }

        return string.Join(" | ", toolbars);
    }

    private static IEnumerable<string> ParseCustom(string? customToolbar)
    {
        if (string.IsNullOrWhiteSpace(customToolbar))
        {
            return [];
        }

        if (!customToolbar.StartsWith('\'') && !customToolbar.StartsWith('"'))
        {
            return [customToolbar];
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>($"[{customToolbar.Replace('\'', '"')}]") ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
