using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

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
/// AngularJS registration only needs this to move its nav link to Blazor. A module that may be installed
/// without one (e.g. a plugin with separate AngularJS and Blazor packages) also sets
/// <see cref="AdminBlazorModule.Module"/>, which is used only when no other registration has its code.
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
    /// The module's landing page: a routable component with an <c>@page</c> route that has no parameters.
    /// Admin pages declare routes without the admin's prefix (see <see cref="AdminBlazorRoutes"/>).
    /// </param>
    /// <param name="route">
    /// Which of the page's routes the nav links to, as declared (e.g. "/dashboard"). Only needed when it has
    /// more than one without parameters.
    /// </param>
    public AdminBlazorModule(string adminModuleCode, Type pageComponent, string? route = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adminModuleCode);
        ArgumentNullException.ThrowIfNull(pageComponent);

        AdminModuleCode = adminModuleCode;
        PageComponent = pageComponent;
        Route = GetRoute(adminModuleCode, pageComponent, route);
    }

    public string AdminModuleCode { get; }

    public Type PageComponent { get; }

    /// <summary>
    /// The page's route as declared, without the admin's prefix.
    /// </summary>
    public string Route { get; }

    /// <summary>
    /// The url the nav links to: <see cref="Route"/> under the admin's prefix. Read when needed, since the
    /// prefix comes from configuration and modules may be created before it is set.
    /// </summary>
    public string Url => Route == "/" ? AdminBlazorRoutes.Root : AdminBlazorRoutes.Root + Route;

    /// <summary>
    /// Optionally, the module itself, for when nothing else registers it: Cofoundry doesn't merge modules
    /// with the same code, so this is skipped when another <see cref="IAdminModuleRegistration"/> has
    /// <see cref="AdminModuleCode"/>. That lets a plugin's Blazor package work alone and also next to its
    /// AngularJS package. Its <see cref="AdminModule.AdminModuleCode"/> must match, and an empty
    /// <see cref="AdminModule.Url"/> is set to <see cref="Url"/> (by <see cref="AdminBlazorModuleRegistry"/>,
    /// when the module is read), so both admins link to the Blazor page.
    /// </summary>
    public AdminModule? Module
    {
        get => _module;
        init
        {
            if (value != null && !value.AdminModuleCode.Equals(AdminModuleCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The module's code {value.AdminModuleCode} doesn't match {AdminModuleCode}.", nameof(Module));
            }

            UsesBlazorUrl = value != null && string.IsNullOrEmpty(value.Url);
            _module = value;
        }
    }

    private readonly AdminModule? _module;

    /// <summary>
    /// True if <see cref="Module"/> had no url, so it links to <see cref="Url"/>.
    /// </summary>
    internal bool UsesBlazorUrl { get; private init; }

    private static string GetRoute(string adminModuleCode, Type pageComponent, string? route)
    {
        var description = $"{pageComponent.FullName}, the page for admin module {adminModuleCode},";

        if (!typeof(IComponent).IsAssignableFrom(pageComponent))
        {
            throw new ArgumentException($"{description} is not a component.", nameof(pageComponent));
        }

        var routes = pageComponent
            .GetCustomAttributes<RouteAttribute>()
            .Select(r => NormaliseRoute(r.Template))
            .Where(t => !t.Contains('{'))
            .ToList();

        if (route != null)
        {
            var normalised = NormaliseRoute(route);
            return routes.FirstOrDefault(r => r.Equals(normalised, StringComparison.OrdinalIgnoreCase))
                ?? throw new ArgumentException($"{description} has no @page route '{route}'.", nameof(route));
        }

        return routes.Count switch
        {
            1 => routes[0],
            0 => throw new ArgumentException($"{description} needs an @page route without parameters.", nameof(pageComponent)),
            _ => throw new ArgumentException($"{description} has several routes; say which one the nav links to.", nameof(route))
        };
    }

    private static string NormaliseRoute(string route) => "/" + route.Trim('/');
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

    /// <summary>
    /// The modules defined by their Blazor registration (<see cref="AdminBlazorModule.Module"/>).
    /// </summary>
    public IEnumerable<AdminModule> GetModuleDefinitions()
    {
        foreach (var module in _modules.Values.Where(m => m.Module != null))
        {
            if (module.UsesBlazorUrl)
            {
                module.Module!.Url = module.Url;
            }

            yield return module.Module!;
        }
    }
}

/// <summary>
/// Adds the modules defined by Blazor registrations (<see cref="AdminBlazorModule.Module"/>) to Cofoundry's
/// modules, for both admins, unless another <see cref="IAdminModuleRegistration"/> already has the code.
/// </summary>
public class AdminBlazorModuleDefinitionRegistration : IAdminModuleRegistration
{
    private readonly AdminBlazorModuleRegistry _registry;
    private readonly IServiceProvider _serviceProvider;

    public AdminBlazorModuleDefinitionRegistration(
        AdminBlazorModuleRegistry registry,
        IServiceProvider serviceProvider
        )
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
    }

    public IEnumerable<AdminModule> GetModules()
    {
        var definitions = _registry.GetModuleDefinitions().ToList();
        if (definitions.Count == 0)
        {
            return definitions;
        }

        // Resolved here rather than injected, since this is one of them.
        var otherCodes = _serviceProvider
            .GetServices<IAdminModuleRegistration>()
            .Where(r => r is not AdminBlazorModuleDefinitionRegistration)
            .SelectMany(r => r.GetModules())
            .Select(m => m.AdminModuleCode)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return definitions.Where(m => !otherCodes.Contains(m.AdminModuleCode));
    }
}
