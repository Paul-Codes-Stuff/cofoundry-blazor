using System.Reflection;
using Microsoft.AspNetCore.Components;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Adds Blazor pages for admin modules: the Blazor admin's equivalent of an AngularJS module registration
/// (<c>IStandardAngularModuleRegistration</c>). Implement it in any assembly Cofoundry scans and it is
/// picked up automatically. For each module it gives the page the nav links to, and the router then finds
/// every routable component in that page's assembly.
/// </summary>
/// <remarks>
/// The module itself (title, menu category, ordering and permission) still comes from an
/// <see cref="IAdminModuleRegistration"/>, as it does for the AngularJS admin. A module that already has an
/// AngularJS registration only needs this to move its nav link to Blazor. A module that is Blazor only
/// implements <see cref="IAdminModuleRegistration"/> too, with its Blazor url as <see cref="AdminModule.Url"/>.
/// Modules without a Blazor page keep linking to their AngularJS url.
/// </remarks>
public interface IAdminBlazorModuleRegistration
{
    IEnumerable<AdminBlazorModule> GetModules();
}

/// <summary>
/// A Blazor page for an admin module. See <see cref="IAdminBlazorModuleRegistration"/>.
/// </summary>
public sealed class AdminBlazorModule
{
    /// <param name="adminModuleCode">The <see cref="AdminModule.AdminModuleCode"/> of the module, e.g. "COFPAG".</param>
    /// <param name="pageComponent">
    /// The module's landing page: a routable component with an <c>@page</c> route under
    /// <see cref="AdminBlazorRoutes.Root"/> that has no parameters.
    /// </param>
    /// <param name="url">
    /// Which of the page's routes the nav links to. Only needed when it has more than one without parameters.
    /// </param>
    public AdminBlazorModule(string adminModuleCode, Type pageComponent, string? url = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminModuleCode);
        ArgumentNullException.ThrowIfNull(pageComponent);

        AdminModuleCode = adminModuleCode;
        PageComponent = pageComponent;
        Url = GetUrl(adminModuleCode, pageComponent, url);
    }

    public string AdminModuleCode { get; }

    public Type PageComponent { get; }

    /// <summary>
    /// The page's route, which the nav links to.
    /// </summary>
    public string Url { get; }

    private static string GetUrl(string adminModuleCode, Type pageComponent, string? url)
    {
        var description = $"{pageComponent.FullName}, the page for admin module {adminModuleCode},";

        if (!typeof(IComponent).IsAssignableFrom(pageComponent))
        {
            throw new ArgumentException($"{description} is not a component.", nameof(pageComponent));
        }

        var routes = pageComponent
            .GetCustomAttributes<RouteAttribute>()
            .Select(r => r.Template.TrimEnd('/'))
            .Where(t => !t.Contains('{')
                && (t.Equals(AdminBlazorRoutes.Root, StringComparison.OrdinalIgnoreCase)
                    || t.StartsWith(AdminBlazorRoutes.Root + "/", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (url != null)
        {
            return routes.FirstOrDefault(r => r.Equals(url.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"{description} has no @page route '{url}'.", nameof(url));
        }

        return routes.Count switch
        {
            1 => routes[0],
            0 => throw new ArgumentException($"{description} needs an @page route under {AdminBlazorRoutes.Root} without parameters.", nameof(pageComponent)),
            _ => throw new ArgumentException($"{description} has several routes; say which one the nav links to.", nameof(url))
        };
    }
}

/// <summary>
/// Collects every <see cref="IAdminBlazorModuleRegistration"/>: the nav's urls and the assemblies the router
/// and the endpoints scan for pages.
/// </summary>
public class AdminBlazorModuleRegistry
{
    private readonly Dictionary<string, AdminBlazorModule> _modules;

    public AdminBlazorModuleRegistry(IEnumerable<IAdminBlazorModuleRegistration> registrations)
    {
        _modules = new(StringComparer.OrdinalIgnoreCase);

        // Later registrations replace earlier ones, so an application can move a module to its own page.
        foreach (var module in registrations.OrderBy(r => r is DefaultAdminBlazorModuleRegistration ? 0 : 1).SelectMany(r => r.GetModules()))
        {
            _modules[module.AdminModuleCode] = module;
        }

        AdditionalAssemblies = _modules.Values
            .Select(m => m.PageComponent.Assembly)
            .Where(a => a != typeof(App).Assembly)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Assemblies other than the Blazor admin's own that contain module pages.
    /// </summary>
    public IReadOnlyCollection<Assembly> AdditionalAssemblies { get; }

    /// <summary>
    /// The Blazor url of a module, or null if it has no Blazor page.
    /// </summary>
    public string? GetUrl(string adminModuleCode) => _modules.GetValueOrDefault(adminModuleCode)?.Url;
}
