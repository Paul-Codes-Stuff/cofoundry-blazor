using Cofoundry.Web.Admin.Blazor.Components.DataModels;

namespace Cofoundry.Web.Admin.Blazor.Components.CustomEntities;

/// <summary>
/// What every custom entity page needs about its definition, the equivalent of the AngularJS
/// "customEntities.options" each custom entity module was bootstrapped with.
/// </summary>
public sealed class CustomEntityModule
{
    private CustomEntityModule(CustomEntityDefinitionSummary definition, CustomEntityPermissions permissions, CustomEntityDataModelSchema schema)
    {
        Definition = definition;
        Permissions = permissions;
        Schema = schema;
    }

    public CustomEntityDefinitionSummary Definition { get; }

    public CustomEntityPermissions Permissions { get; }

    public CustomEntityDataModelSchema Schema { get; }

    public string Code => Definition.CustomEntityDefinitionCode;

    public string TitleTerm => Definition.Terms.GetValueOrDefault(CustomizableCustomEntityTermKeys.Title) ?? "Title";

    public string UrlSlugTerm => Definition.Terms.GetValueOrDefault(CustomizableCustomEntityTermKeys.UrlSlug) ?? "Url Slug";

    /// <summary>
    /// "Save" for entities that publish automatically, otherwise "Save &amp; Publish".
    /// </summary>
    public string SaveAndPublishText => Definition.AutoPublish ? "Save" : "Save & Publish";

    /// <summary>
    /// The url can only change when there is a slug to edit or a locale to move to.
    /// </summary>
    public bool CanChangeUrl => !Definition.AutoGenerateUrlSlug || Definition.HasLocale;

    public bool HasProperties => Schema.DataModelProperties.Count > 0;

    /// <summary>
    /// The property marked [PreviewImage], shown as a thumbnail in the list.
    /// </summary>
    public DynamicDataModelSchemaProperty? PreviewImageProperty => FindPreviewProperty(DataModelAttributes.PreviewImage);

    /// <summary>
    /// The property marked [PreviewDescription], shown as a column in the list.
    /// </summary>
    public DynamicDataModelSchemaProperty? PreviewDescriptionProperty => FindPreviewProperty(DataModelAttributes.PreviewDescription);

    /// <summary>
    /// A new data model with its default values, for the create page. The schema query maps a fresh
    /// instance each time, so it is safe to edit.
    /// </summary>
    public ICustomEntityDataModel CreateDefaultModel()
    {
        return Schema.DefaultValue.Value as ICustomEntityDataModel
            ?? (ICustomEntityDataModel)Activator.CreateInstance(Definition.DataModelType)!;
    }

    /// <summary>
    /// Returns null when there is no custom entity definition with that code.
    /// </summary>
    public static async Task<CustomEntityModule?> LoadAsync(IAdminBlazorExecutor executor, string customEntityDefinitionCode)
    {
        var definition = await executor.QueryAsync(new GetCustomEntityDefinitionSummaryByCodeQuery(customEntityDefinitionCode));

        if (definition == null)
        {
            return null;
        }

        var permissions = await CustomEntityPermissions.LoadAsync(executor, definition.CustomEntityDefinitionCode);
        var schema = await executor.QueryAsync(new GetCustomEntityDataModelSchemaDetailsByDefinitionCodeQuery(definition.CustomEntityDefinitionCode));

        return schema == null ? null : new CustomEntityModule(definition, permissions, schema);
    }

    /// <summary>
    /// Reads a data model property for the list columns.
    /// </summary>
    public static object? GetModelValue(ICustomEntityDataModel? model, DynamicDataModelSchemaProperty property)
    {
        return model?.GetType().GetProperty(property.Name)?.GetValue(model);
    }

    private DynamicDataModelSchemaProperty? FindPreviewProperty(string attribute)
    {
        return Schema.DataModelProperties.FirstOrDefault(p => p.AdditionalAttributes.Keys
            .Any(k => string.Equals(k, attribute, StringComparison.OrdinalIgnoreCase)));
    }
}
