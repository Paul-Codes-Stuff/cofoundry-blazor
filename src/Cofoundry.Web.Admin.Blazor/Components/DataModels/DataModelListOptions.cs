using System.Collections;
using System.Globalization;
using System.Text.Json;
using Cofoundry.Web.Admin.Blazor.Components.Ui;
using Microsoft.JSInterop;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// The options of a [SelectList], [RadioList] or [CheckboxList] property. An enum or IListOptionSource
/// puts them in the schema ("Options"). An IListOptionApiSource gives a relative path instead
/// ("OptionsApi"), which is fetched in the browser so it runs with the user's cookies, as the AngularJS
/// optionSourceService did. Values are compared as invariant strings, as the AngularJS fields compared
/// them loosely, so an int property matches 1 and "1" and an enum matches its name.
/// </summary>
public static class DataModelListOptions
{
    private const string OptionsKey = "Options";
    private const string OptionsApiKey = "OptionsApi";
    private const string OptionNameKey = "OptionName";
    private const string OptionValueKey = "OptionValue";

    // optionSourceService unwrapped up to three "data" properties.
    private const int MaxDataDepth = 3;

    public static async Task<IReadOnlyList<SelectOption<string?>>> LoadAsync(DataModelField field, IJSRuntime jsRuntime)
    {
        var apiPath = field.GetAttribute<string>(OptionsApiKey);

        if (string.IsNullOrEmpty(apiPath))
        {
            return field.GetAttribute<object>(OptionsKey) is IEnumerable options
                ? options.OfType<ListOption>().Select(o => new SelectOption<string?>(ToKey(o.Value), o.Text)).ToList()
                : [];
        }

        await using var module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", AdminAssetPaths.OptionSourceScript);
        var json = await module.InvokeAsync<string>("getJson", apiPath);

        return ParseApiOptions(json, field.GetAttribute<string>(OptionNameKey), field.GetAttribute<string>(OptionValueKey));
    }

    /// <summary>
    /// The string an option value or a property value is compared by.
    /// </summary>
    public static string? ToKey(object? value) => value switch
    {
        null => null,
        string text => text,
        Enum enumValue => enumValue.ToString(),
        JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
        JsonElement { ValueKind: JsonValueKind.Null } => null,
        JsonElement element => element.GetRawText(),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()
    };

    private static List<SelectOption<string?>> ParseApiOptions(string json, string? nameField, string? valueField)
    {
        using var document = JsonDocument.Parse(json);
        var data = document.RootElement;

        for (var depth = 0; depth < MaxDataDepth && data.ValueKind == JsonValueKind.Object && TryGetProperty(data, "data", out var inner); depth++)
        {
            data = inner;
        }

        if (data.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var options = new List<SelectOption<string?>>();

        foreach (var item in data.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var value = valueField != null && TryGetProperty(item, valueField, out var v) ? ToKey(v) : null;
            var text = nameField != null && TryGetProperty(item, nameField, out var n) ? ToKey(n) : null;
            options.Add(new SelectOption<string?>(value, text ?? value ?? string.Empty));
        }

        return options;
    }

    /// <summary>
    /// The api's json may be camel or pascal cased; the schema gives the field names camelized.
    /// </summary>
    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
