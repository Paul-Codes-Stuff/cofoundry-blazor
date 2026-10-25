using Cofoundry.Web.Admin.Blazor.Components.Ui;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// Loads a list field's options (see <see cref="DataModelListOptions"/>) the first time it is shown.
/// </summary>
public abstract class DataModelListFieldBase : DataModelFieldComponentBase
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    protected IReadOnlyList<SelectOption<string?>>? Options { get; private set; }

    /// <summary>
    /// Set when an api option source couldn't be loaded.
    /// </summary>
    protected string? LoadError { get; private set; }

    protected string? DefaultItemText => Field.GetAttribute<string>("DefaultItemText");

    protected override async Task OnParametersSetAsync()
    {
        if (Options != null || LoadError != null)
        {
            return;
        }

        try
        {
            Options = await DataModelListOptions.LoadAsync(Field, JSRuntime);
        }
        catch (Exception ex) when (ex is JSException or System.Text.Json.JsonException)
        {
            LoadError = $"The options for {Field.Title} could not be loaded: {ex.Message}";
        }
    }
}
