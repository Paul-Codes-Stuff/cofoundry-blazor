using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// The set of page permissions the current user holds, resolved in one round trip.
/// </summary>
public sealed record PagePermissions(
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete,
    bool CanPublish,
    bool CanUpdateUrl,
    bool CanManageAccessRules)
{
    public static async Task<PagePermissions> LoadAsync(IAdminBlazorExecutor executor)
    {
        return await executor.WithServicesAsync(async services =>
        {
            var permissions = services.GetRequiredService<IPermissionValidationService>();

            return new PagePermissions(
                await permissions.HasPermissionAsync<PageCreatePermission>(),
                await permissions.HasPermissionAsync<PageUpdatePermission>(),
                await permissions.HasPermissionAsync<PageDeletePermission>(),
                await permissions.HasPermissionAsync<PagePublishPermission>(),
                await permissions.HasPermissionAsync<PageUpdateUrlPermission>(),
                await permissions.HasPermissionAsync<PageAccessRuleManagePermission>()
                );
        });
    }
}

/// <summary>
/// The create/update/delete permissions for an asset module (images or documents).
/// </summary>
public sealed record AssetPermissions(bool CanCreate, bool CanUpdate, bool CanDelete)
{
    public static Task<AssetPermissions> LoadImagesAsync(IAdminBlazorExecutor executor) =>
        LoadAsync<ImageAssetCreatePermission, ImageAssetUpdatePermission, ImageAssetDeletePermission>(executor);

    public static Task<AssetPermissions> LoadDocumentsAsync(IAdminBlazorExecutor executor) =>
        LoadAsync<DocumentAssetCreatePermission, DocumentAssetUpdatePermission, DocumentAssetDeletePermission>(executor);

    private static async Task<AssetPermissions> LoadAsync<TCreate, TUpdate, TDelete>(IAdminBlazorExecutor executor)
        where TCreate : IPermissionApplication, new()
        where TUpdate : IPermissionApplication, new()
        where TDelete : IPermissionApplication, new()
    {
        return await executor.WithServicesAsync(async services =>
        {
            var permissions = services.GetRequiredService<IPermissionValidationService>();

            return new AssetPermissions(
                await permissions.HasPermissionAsync<TCreate>(),
                await permissions.HasPermissionAsync<TUpdate>(),
                await permissions.HasPermissionAsync<TDelete>()
                );
        });
    }
}
