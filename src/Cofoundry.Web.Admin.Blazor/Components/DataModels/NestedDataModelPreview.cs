using System.Reflection;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// Port of the AngularJS ModelPreviewFieldset: the [PreviewTitle], [PreviewDescription] and
/// [PreviewImage] properties of a nested data model, which the collection fields show as table columns.
/// </summary>
public sealed class NestedDataModelPreview
{
    private const string DefaultTitlePropertyName = "Title";

    public NestedDataModelPreview(IDynamicDataModelSchema schema)
    {
        TitleProperty = FindProperty(schema, DataModelAttributes.PreviewTitle);
        DescriptionProperty = FindProperty(schema, DataModelAttributes.PreviewDescription);
        ImageProperty = FindProperty(schema, DataModelAttributes.PreviewImage);
    }

    public DynamicDataModelSchemaProperty? TitleProperty { get; }

    public DynamicDataModelSchemaProperty? DescriptionProperty { get; }

    public DynamicDataModelSchemaProperty? ImageProperty { get; }

    public bool HasFields => TitleProperty != null || DescriptionProperty != null || ImageProperty != null;

    /// <summary>
    /// The title column shows when there's a [PreviewTitle], or when there are no preview fields at all.
    /// </summary>
    public bool ShowTitle => TitleProperty != null || !HasFields;

    public string TitleTerm => TitleProperty?.DisplayName ?? "Title";

    /// <summary>
    /// The [PreviewTitle] value, else a "Title" property, else "Item n".
    /// </summary>
    public string GetTitle(object model, int index)
    {
        var title = GetValue(model, TitleProperty?.Name ?? DefaultTitlePropertyName)?.ToString();

        return string.IsNullOrWhiteSpace(title) ? $"Item {index + 1}" : title;
    }

    public string? GetDescription(object model)
    {
        return DescriptionProperty == null ? null : GetValue(model, DescriptionProperty.Name)?.ToString();
    }

    public int? GetImageId(object model)
    {
        return ImageProperty != null && GetValue(model, ImageProperty.Name) is int id && id > 0 ? id : null;
    }

    /// <summary>
    /// Loads the preview thumbnails for a set of image ids in one query, as the AngularJS
    /// ImagePreviewFieldCollection did.
    /// </summary>
    public static async Task<IReadOnlyDictionary<int, ImageAssetRenderDetails>> LoadImagesAsync(
        IAdminBlazorExecutor executor,
        IEnumerable<int?> imageIds)
    {
        var ids = imageIds
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        return ids.Count == 0
            ? new Dictionary<int, ImageAssetRenderDetails>()
            : await executor.QueryAsync(new GetImageAssetRenderDetailsByIdRangeQuery(ids));
    }

    private static object? GetValue(object model, string propertyName)
    {
        return model.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)?.GetValue(model);
    }

    private static DynamicDataModelSchemaProperty? FindProperty(IDynamicDataModelSchema schema, string attribute)
    {
        return schema.DataModelProperties.FirstOrDefault(p => p.AdditionalAttributes.Keys
            .Any(k => string.Equals(k, attribute, StringComparison.OrdinalIgnoreCase)));
    }
}
