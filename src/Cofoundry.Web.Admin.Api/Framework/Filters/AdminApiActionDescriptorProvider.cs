using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace Cofoundry.Web.Admin.Api;

/// <summary>
/// Removes the Visual Editor's global result filter from the API's actions. While Cofoundry.Web.Admin is
/// installed, it wraps every result outside its own directory and "/api" to inject the editor into html,
/// and writes the API's json back with a byte order mark. Its exclusion rules are a Cofoundry.Web.Admin
/// interface, which this package doesn't reference, so the filter is matched by name.
/// </summary>
public class AdminApiActionDescriptorProvider : IActionDescriptorProvider
{
    private const string VisualEditorFilterTypeName = "Cofoundry.Web.Admin.Internal.VisualEditorContentFilter";

    // After the controller action descriptors are built (ControllerActionDescriptorProvider is -1000).
    public int Order => 0;

    public void OnProvidersExecuting(ActionDescriptorProviderContext context)
    {
    }

    public void OnProvidersExecuted(ActionDescriptorProviderContext context)
    {
        foreach (var action in context.Results)
        {
            if (!action.RouteValues.TryGetValue("area", out var area) || area != AdminApiRouteConstants.AreaName) continue;

            var visualEditorFilters = action.FilterDescriptors
                .Where(f => f.Filter is TypeFilterAttribute t && t.ImplementationType.FullName == VisualEditorFilterTypeName)
                .ToList();

            foreach (var filter in visualEditorFilters)
            {
                action.FilterDescriptors.Remove(filter);
            }
        }
    }
}
