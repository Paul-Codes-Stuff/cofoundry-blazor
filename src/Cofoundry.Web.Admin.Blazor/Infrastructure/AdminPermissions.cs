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
/// The create/update/delete permissions for a simple entity module (images, documents, roles).
/// </summary>
public sealed record CrudPermissions(bool CanCreate, bool CanUpdate, bool CanDelete)
{
    public static Task<CrudPermissions> LoadImagesAsync(IAdminBlazorExecutor executor) =>
        LoadAsync<ImageAssetCreatePermission, ImageAssetUpdatePermission, ImageAssetDeletePermission>(executor);

    public static Task<CrudPermissions> LoadDocumentsAsync(IAdminBlazorExecutor executor) =>
        LoadAsync<DocumentAssetCreatePermission, DocumentAssetUpdatePermission, DocumentAssetDeletePermission>(executor);

    public static Task<CrudPermissions> LoadRolesAsync(IAdminBlazorExecutor executor) =>
        LoadAsync<RoleCreatePermission, RoleUpdatePermission, RoleDeletePermission>(executor);

    private static async Task<CrudPermissions> LoadAsync<TCreate, TUpdate, TDelete>(IAdminBlazorExecutor executor)
        where TCreate : IPermissionApplication, new()
        where TUpdate : IPermissionApplication, new()
        where TDelete : IPermissionApplication, new()
    {
        return await executor.WithServicesAsync(async services =>
        {
            var permissions = services.GetRequiredService<IPermissionValidationService>();

            return new CrudPermissions(
                await permissions.HasPermissionAsync<TCreate>(),
                await permissions.HasPermissionAsync<TUpdate>(),
                await permissions.HasPermissionAsync<TDelete>()
                );
        });
    }
}

/// <summary>
/// Permissions for managing the users of one user area. Cofoundry admin users and users of other
/// areas have separate permission sets, as in the AngularJS admin (COFUSR / COFUSN).
/// </summary>
public sealed record UserPermissions(
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete,
    bool CanResetPassword,
    bool CanReadRoles)
{
    public static async Task<UserPermissions> LoadAsync(IAdminBlazorExecutor executor, string userAreaCode)
    {
        var isAdminArea = userAreaCode == CofoundryAdminUserArea.Code;

        return await executor.WithServicesAsync(async services =>
        {
            var permissions = services.GetRequiredService<IPermissionValidationService>();

            return isAdminArea
                ? new UserPermissions(
                    await permissions.HasPermissionAsync<CofoundryUserCreatePermission>(),
                    await permissions.HasPermissionAsync<CofoundryUserUpdatePermission>(),
                    await permissions.HasPermissionAsync<CofoundryUserDeletePermission>(),
                    await permissions.HasPermissionAsync<CofoundryUserResetPasswordPermission>(),
                    await permissions.HasPermissionAsync<RoleReadPermission>())
                : new UserPermissions(
                    await permissions.HasPermissionAsync<NonCofoundryUserCreatePermission>(),
                    await permissions.HasPermissionAsync<NonCofoundryUserUpdatePermission>(),
                    await permissions.HasPermissionAsync<NonCofoundryUserDeletePermission>(),
                    await permissions.HasPermissionAsync<NonCofoundryUserResetPasswordPermission>(),
                    await permissions.HasPermissionAsync<RoleReadPermission>());
        });
    }
}
