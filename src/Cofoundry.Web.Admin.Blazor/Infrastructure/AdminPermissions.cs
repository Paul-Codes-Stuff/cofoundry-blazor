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
/// The page directory permissions the directories module checks.
/// </summary>
public sealed record DirectoryPermissions(bool CanCreate, bool CanUpdate, bool CanDelete, bool CanUpdateUrl)
{
    public static async Task<DirectoryPermissions> LoadAsync(IAdminBlazorExecutor executor)
    {
        return await executor.WithServicesAsync(async services =>
        {
            var permissions = services.GetRequiredService<IPermissionValidationService>();

            return new DirectoryPermissions(
                await permissions.HasPermissionAsync<PageDirectoryCreatePermission>(),
                await permissions.HasPermissionAsync<PageDirectoryUpdatePermission>(),
                await permissions.HasPermissionAsync<PageDirectoryDeletePermission>(),
                await permissions.HasPermissionAsync<PageDirectoryUpdateUrlPermission>()
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

/// <summary>
/// Permissions for one custom entity definition. Each definition has its own permission set, coded
/// by the definition code (e.g. "SIMBLPCOMCRT"), as in the AngularJS custom entities module.
/// </summary>
public sealed record CustomEntityPermissions(
    bool CanCreate,
    bool CanUpdate,
    bool CanDelete,
    bool CanPublish,
    bool CanUpdateUrl)
{
    public static async Task<CustomEntityPermissions> LoadAsync(IAdminBlazorExecutor executor, string customEntityDefinitionCode)
    {
        return await executor.WithServicesAsync(async services =>
        {
            var permissions = services.GetRequiredService<IPermissionValidationService>();

            return new CustomEntityPermissions(
                await permissions.HasCustomEntityPermissionAsync<CustomEntityCreatePermission>(customEntityDefinitionCode),
                await permissions.HasCustomEntityPermissionAsync<CustomEntityUpdatePermission>(customEntityDefinitionCode),
                await permissions.HasCustomEntityPermissionAsync<CustomEntityDeletePermission>(customEntityDefinitionCode),
                await permissions.HasCustomEntityPermissionAsync<CustomEntityPublishPermission>(customEntityDefinitionCode),
                await permissions.HasCustomEntityPermissionAsync<CustomEntityUpdateUrlPermission>(customEntityDefinitionCode)
                );
        });
    }
}
