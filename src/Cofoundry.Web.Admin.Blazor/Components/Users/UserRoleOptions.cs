using Cofoundry.Web.Admin.Blazor.Components.Ui;

namespace Cofoundry.Web.Admin.Blazor.Components.Users;

/// <summary>
/// The role dropdown options for a user area, as the AngularJS roleService.getSelectionList.
/// </summary>
public static class UserRoleOptions
{
    public static async Task<IReadOnlyList<SelectOption<int?>>> LoadAsync(IAdminBlazorExecutor executor, string userAreaCode)
    {
        var roles = await executor.QueryAsync(new SearchRolesQuery
        {
            UserAreaCode = userAreaCode,
            ExcludeAnonymous = true,
            PageSize = 200
        });

        return roles.Items
            .Select(r => new SelectOption<int?>(r.RoleId, r.Title))
            .ToList();
    }
}
