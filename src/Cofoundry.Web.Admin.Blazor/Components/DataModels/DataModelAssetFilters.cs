using System.Collections;
using Cofoundry.Web.Admin.Blazor.Components.Documents;
using Cofoundry.Web.Admin.Blazor.Components.Images;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// Reads the picker restrictions that [Image], [ImageCollection], [Document] and [DocumentCollection]
/// add to the schema, as the AngularJS fields read them from their cms-* attributes.
/// </summary>
public static class DataModelAssetFilters
{
    public static ImageAssetFilter Image(DataModelField field) => new()
    {
        Tags = Tags(field),
        Width = Size(field, "Width"),
        Height = Size(field, "Height"),
        MinWidth = Size(field, "MinWidth"),
        MinHeight = Size(field, "MinHeight")
    };

    public static DocumentAssetFilter Document(DataModelField field) => new()
    {
        Tags = Tags(field),
        FileExtension = NullIfEmpty(field.GetAttribute<string>("FileExtension")),
        FileExtensions = NullIfEmpty(field.GetAttribute<string>("FileExtensions"))
    };

    /// <summary>
    /// The attributes store tags as a string array.
    /// </summary>
    private static string? Tags(DataModelField field) => field.GetAttribute<object>("Tags") switch
    {
        string tags => NullIfEmpty(tags),
        IEnumerable tags => ImageAssetFilter.JoinTags(tags.OfType<string>()),
        _ => null
    };

    private static int? Size(DataModelField field, string key) => field.GetAttribute<int?>(key) is > 0 and var size ? size : null;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
