using Microsoft.AspNetCore.Components;

namespace Cofoundry.Web.Admin.Blazor.Components.CustomEntities;

/// <summary>
/// The custom entity pages each have two routes: "/{slug}/..." from the definition's plural name, as the
/// AngularJS admin's urls, and "/custom-entities/{code}/..." (see <see cref="AdminBlazorRoutes.CustomEntities"/>).
/// Both put the segment in the same route parameter, because the router keeps a parameter that the new route
/// doesn't set when it reuses the page for another url.
/// <para>
/// The slug routes match any first segment, including those of the static SSR pages (My Account, sign in,
/// setup), which the interactive router doesn't list. Without the slug routes it found no page there and
/// loaded the url from the server, so these pages do that for a segment that belongs to another route.
/// </para>
/// </summary>
public static class CustomEntityRoute
{
    /// <summary>
    /// The definition code that the page's route parameter names, or null if it names none. On a code url of a
    /// definition that has a slug url, also moves the browser to the slug url (the rest of the url is kept), and
    /// on another route's url loads that from the server.
    /// </summary>
    public static string? GetDefinitionCode(NavigationManager navigation, string routeValue)
    {
        var relativePath = navigation.ToBaseRelativePath(navigation.Uri);
        var codePath = AdminBlazorRoutes.CustomEntitiesByCodeSegment + "/" + routeValue;

        if (!relativePath.StartsWith(codePath, StringComparison.OrdinalIgnoreCase))
        {
            var code = AdminBlazorRoutes.GetCustomEntityDefinitionCode(routeValue);
            if (code == null && AdminBlazorRoutes.IsReservedSegment(routeValue))
            {
                navigation.Refresh(forceReload: true);
            }

            return code;
        }

        var url = AdminBlazorRoutes.CustomEntities(routeValue);
        if (!url.EndsWith("/" + codePath, StringComparison.OrdinalIgnoreCase))
        {
            navigation.NavigateTo(url + relativePath[codePath.Length..], replace: true);
        }

        return routeValue;
    }
}
