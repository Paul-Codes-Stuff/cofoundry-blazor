namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// What the visual editor knows about the page being viewed. It's written into the page as json for
/// <c>js/visual-editor.js</c>, which sends it to the frame with each action, and the toolbar renders from it.
/// The AngularJS editor serialized the whole <see cref="IPageResponseData"/> (the page's view model included)
/// as <c>Cofoundry.PageResponseData</c>; this is only what the editor uses.
/// </summary>
public class VisualEditorPageData
{
    public VisualEditorMode VisualEditorMode { get; init; }

    /// <summary>
    /// True when editing a custom entity's regions, false when editing the page (or a custom entity
    /// page's template).
    /// </summary>
    public bool IsCustomEntityRoute { get; init; }

    public int PageId { get; init; }

    public int? CustomEntityId { get; init; }

    public string? CustomEntityDefinitionCode { get; init; }

    /// <summary>
    /// "Page", or the custom entity definition's singular name.
    /// </summary>
    public string EntityNameSingular { get; init; } = "Page";

    /// <summary>
    /// The page or custom entity version being shown, which blocks are added to.
    /// </summary>
    public int VersionId { get; init; }

    /// <summary>
    /// The page's or custom entity's publish status.
    /// </summary>
    public PublishStatus PublishStatus { get; init; }

    /// <summary>
    /// Whether the page version shown is the latest published one.
    /// </summary>
    public bool IsLatestPublishedVersion { get; init; }

    public bool HasDraftVersion { get; init; }

    /// <summary>
    /// The page or custom entity update permission.
    /// </summary>
    public bool CanUpdate { get; init; }

    /// <summary>
    /// The page or custom entity publish permission.
    /// </summary>
    public bool CanPublish { get; init; }

    public static VisualEditorPageData Create(IPageResponseData data, IPermissionValidationService permissionValidationService)
    {
        ArgumentNullException.ThrowIfNull(data);
        EntityInvalidOperationException.ThrowIfNull(data, data.CofoundryAdminUserContext);

        var user = data.CofoundryAdminUserContext;
        var routingInfo = data.PageRoutingInfo;
        var isCustomEntityRoute = data.Version is CustomEntityVersionRoute;
        bool canUpdate, canPublish;

        if (isCustomEntityRoute)
        {
            EntityInvalidOperationException.ThrowIfNull(data, data.CustomEntityDefinition);
            var code = data.CustomEntityDefinition.CustomEntityDefinitionCode;
            canUpdate = permissionValidationService.HasCustomEntityPermission<CustomEntityUpdatePermission>(code, user);
            canPublish = permissionValidationService.HasCustomEntityPermission<CustomEntityPublishPermission>(code, user);
        }
        else
        {
            canUpdate = permissionValidationService.HasPermission<PageUpdatePermission>(user);
            canPublish = permissionValidationService.HasPermission<PagePublishPermission>(user);
        }

        return new VisualEditorPageData
        {
            VisualEditorMode = data.VisualEditorMode,
            IsCustomEntityRoute = isCustomEntityRoute,
            PageId = routingInfo.PageRoute.PageId,
            CustomEntityId = routingInfo.CustomEntityRoute?.CustomEntityId,
            CustomEntityDefinitionCode = data.CustomEntityDefinition?.CustomEntityDefinitionCode,
            EntityNameSingular = isCustomEntityRoute ? data.CustomEntityDefinition!.Name : "Page",
            VersionId = data.Version.VersionId,
            PublishStatus = isCustomEntityRoute ? routingInfo.CustomEntityRoute!.PublishStatus : routingInfo.PageRoute.PublishStatus,
            IsLatestPublishedVersion = data.PageVersion.IsLatestPublishedVersion,
            HasDraftVersion = data.HasDraftVersion,
            CanUpdate = canUpdate,
            CanPublish = canPublish
        };
    }
}
