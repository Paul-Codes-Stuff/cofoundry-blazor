using Cofoundry.Web.Admin.Blazor.Components.Dashboard;
using Cofoundry.Web.Admin.Blazor.Components.Directories;
using Cofoundry.Web.Admin.Blazor.Components.Documents;
using Cofoundry.Web.Admin.Blazor.Components.Images;
using Cofoundry.Web.Admin.Blazor.Components.Pages;
using Cofoundry.Web.Admin.Blazor.Components.PageTemplates;
using Cofoundry.Web.Admin.Blazor.Components.Roles;
using Cofoundry.Web.Admin.Blazor.Components.Settings;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// The built-in modules ported so far. Custom entity and users modules aren't listed: there's one per
/// definition or user area, so the nav maps them itself.
/// </summary>
public class DefaultAdminBlazorModuleRegistration : IAdminBlazorModuleRegistration
{
    public IEnumerable<AdminBlazorModule> GetModules()
    {
        yield return new("COFDSH", typeof(Dashboard), AdminBlazorRoutes.Dashboard);
        yield return new("COFPAG", typeof(PageList));
        yield return new("COFIMG", typeof(ImageList));
        yield return new("COFDOC", typeof(DocumentList));
        yield return new("COFSET", typeof(SiteSettingsPage));
        yield return new("COFROL", typeof(RoleList));
        yield return new("COFDIR", typeof(DirectoryList));
        yield return new("COFPGT", typeof(PageTemplateList));
    }
}
