using Microsoft.AspNetCore.Http;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Keeps the visual editor out of some requests, e.g. an MVC area of your own that renders html but isn't
/// part of the site. Implementations are registered automatically, and a request is excluded when any rule
/// says so. Like Cofoundry.Web.Admin's interface of the same name, for the Blazor admin's visual editor.
/// </summary>
public interface IVisualEditorRequestExclusionRule
{
    /// <summary>
    /// Returns true if the visual editor should not be injected into the response to this request.
    /// </summary>
    bool ShouldExclude(HttpRequest request);
}

/// <summary>
/// Excludes both admins and anything under "/api".
/// </summary>
public class DefaultVisualEditorRequestExclusionRule : IVisualEditorRequestExclusionRule
{
    private readonly AdminSettings _adminSettings;

    public DefaultVisualEditorRequestExclusionRule(AdminSettings adminSettings)
    {
        _adminSettings = adminSettings;
    }

    public bool ShouldExclude(HttpRequest request)
    {
        return GetExcludedPaths().Any(p => request.Path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));
    }

    private IEnumerable<PathString> GetExcludedPaths()
    {
        yield return "/" + _adminSettings.DirectoryName;
        yield return AdminBlazorRoutes.Root;
        yield return "/api";
    }
}
