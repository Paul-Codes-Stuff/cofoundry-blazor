using Cofoundry.Plugins.ErrorLogging.Admin.Blazor.Components;
using Cofoundry.Plugins.ErrorLogging.Domain;

namespace Cofoundry.Plugins.ErrorLogging.Admin.Blazor;

/// <summary>
/// Adds the error log module to the Blazor admin. The module is described here as well as in the AngularJS
/// package (Cofoundry.Plugins.ErrorLogging.Admin), so this package works on its own; when both are
/// installed the AngularJS description is used and only the nav link moves to Blazor.
/// </summary>
public class ErrorsBlazorModuleRegistration : IAdminBlazorModuleRegistration
{
    public const string AdminModuleCode = "COFERR";

    public IEnumerable<AdminBlazorModule> GetModules()
    {
        yield return new(AdminModuleCode, typeof(ErrorList))
        {
            Module = new AdminModule()
            {
                AdminModuleCode = AdminModuleCode,
                Title = "Error Log",
                Description = "View the site error logs.",
                MenuCategory = AdminModuleMenuCategory.Settings,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Tertiary,
                RestrictedToPermission = new ErrorLogReadPermission()
            }
        };
    }
}
