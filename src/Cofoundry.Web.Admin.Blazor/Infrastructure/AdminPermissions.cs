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
