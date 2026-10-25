namespace Cofoundry.Web.Admin.Blazor.Components.CustomEntities;

/// <summary>
/// Status and version labels, as the AngularJS custom entity pages built them.
/// </summary>
public static class CustomEntityLabels
{
    /// <summary>
    /// "Published", "Draft", or "Pending Publish" when published with a future publish date.
    /// </summary>
    public static string PublishStatus(PublishStatus status, DateTime? publishDate)
    {
        if (status == Domain.PublishStatus.Published && publishDate.HasValue && publishDate.Value > DateTime.UtcNow)
        {
            return "Pending Publish";
        }

        return status.ToString();
    }

    /// <summary>
    /// "Draft", "V3", or "V3 (Published)" for the latest published version.
    /// </summary>
    public static string Version(CustomEntityVersionSummary version, CustomEntityDetails entity)
    {
        if (version.WorkFlowStatus == WorkFlowStatus.Draft)
        {
            return version.WorkFlowStatus.ToString();
        }

        var label = "V" + version.DisplayVersion;

        return version.IsLatestPublishedVersion
            ? $"{label} ({PublishStatus(entity.PublishStatus, entity.PublishDate)})"
            : label;
    }
}

/// <summary>
/// Visual editor urls, as the AngularJS UrlLibrary built them for custom entities.
/// </summary>
public static class CustomEntityUrls
{
    public static string VisualEditor(string fullUrlPath) => fullUrlPath + "?mode=edit&edittype=entity";

    public static string BrowseVersion(CustomEntityDetails entity, CustomEntityVersionSummary version)
    {
        var url = entity.FullUrlPath + "?";
        var isPublished = entity.PublishStatus == PublishStatus.Published && entity.HasPublishedVersion;

        if (version.WorkFlowStatus == WorkFlowStatus.Draft)
        {
            url += "mode=preview";
        }
        else if (version.IsLatestPublishedVersion && isPublished)
        {
            url += "mode=live";
        }
        else
        {
            url += "version=" + version.CustomEntityVersionId;
        }

        return url + "&edittype=entity";
    }
}

/// <summary>
/// Additional attribute keys set by [CustomEntity] and [CustomEntityCollection].
/// </summary>
public static class CustomEntityFieldAttributes
{
    public const string DefinitionCode = "CustomEntityDefinitionCode";
    public const string Orderable = "Orderable";
}
