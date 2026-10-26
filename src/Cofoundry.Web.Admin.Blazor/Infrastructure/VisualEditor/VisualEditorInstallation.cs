using Microsoft.AspNetCore.Mvc;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Decides which visual editor the site's pages get. Cofoundry.Web.Admin adds its AngularJS editor as a
/// global MVC filter; while that package is installed it keeps it, unless
/// <see cref="AdminBlazorSettings.UseVisualEditor"/> is on, in which case its filter is removed and this
/// package's is added. Without Cofoundry.Web.Admin this package's editor is always used. Either way the
/// editor is off when pages or the admin are disabled, or <see cref="AdminSettings.AutoInjectVisualEditor"/>
/// is off.
/// </summary>
public static class VisualEditorInstallation
{
    private const string AngularAdminAssemblyName = "Cofoundry.Web.Admin";
    private const string AngularVisualEditorFilterTypeName = "Cofoundry.Web.Admin.Internal.VisualEditorContentFilter";

    public static bool IsAngularAdminInstalled()
    {
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(a => a.GetName().Name == AngularAdminAssemblyName);
    }

    /// <summary>
    /// Whether this package's visual editor is injected into the site's pages.
    /// </summary>
    public static bool IsEnabled(AdminSettings adminSettings, PagesSettings pagesSettings, AdminBlazorSettings adminBlazorSettings)
    {
        return !adminSettings.Disabled
            && !pagesSettings.Disabled
            && adminSettings.AutoInjectVisualEditor
            && (adminBlazorSettings.UseVisualEditor || !IsAngularAdminInstalled());
    }

    /// <summary>
    /// Swaps the AngularJS editor's filter for this one's when it's enabled. Runs after every
    /// <see cref="IMvcOptionsConfiguration"/>, so the AngularJS filter has been added by then.
    /// </summary>
    public static void ConfigureFilters(MvcOptions options, AdminSettings adminSettings, PagesSettings pagesSettings, AdminBlazorSettings adminBlazorSettings)
    {
        if (!IsEnabled(adminSettings, pagesSettings, adminBlazorSettings))
        {
            return;
        }

        var angularFilters = options.Filters
            .Where(f => f is TypeFilterAttribute t && t.ImplementationType.FullName == AngularVisualEditorFilterTypeName)
            .ToList();

        foreach (var filter in angularFilters)
        {
            options.Filters.Remove(filter);
        }

        options.Filters.Add(typeof(VisualEditorContentFilter));
    }
}
