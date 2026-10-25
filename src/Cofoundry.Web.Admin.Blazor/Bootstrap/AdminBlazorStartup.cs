using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Adds the Razor Components services required to host the Blazor admin.
/// </summary>
public class AdminBlazorServiceConfigurationTask : IStartupServiceConfigurationTask
{
    public void ConfigureServices(IMvcBuilder mvcBuilder)
    {
        mvcBuilder.Services
            .AddRazorComponents()
            .AddInteractiveServerComponents();

        mvcBuilder.Services.AddCascadingAuthenticationState();
    }
}

/// <summary>
/// Razor Component endpoints carry anti-forgery metadata, so the anti-forgery middleware has
/// to run after authentication/authorization and before endpoints are executed.
/// </summary>
public class AdminBlazorAntiforgeryStartupConfigurationTask
    : IStartupConfigurationTask
    , IRunAfterStartupConfigurationTask
{
    public int Ordering => (int)StartupTaskOrdering.Early;

    public IReadOnlyCollection<Type> RunAfter => [typeof(AuthStartupConfigurationTask)];

    public void Configure(IApplicationBuilder app)
    {
        app.UseAntiforgery();
    }
}

/// <summary>
/// Maps the Blazor admin. Registered early so that it takes precedence over Cofoundry's
/// catch-all page route.
/// </summary>
public class AdminBlazorRouteRegistration : IOrderedRouteRegistration
{
    private readonly AdminSettings _adminSettings;

    public AdminBlazorRouteRegistration(AdminSettings adminSettings)
    {
        _adminSettings = adminSettings;
    }

    public int Ordering => (int)RouteRegistrationOrdering.Early;

    public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
    {
        if (_adminSettings.Disabled)
        {
            return;
        }

        routeBuilder
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
    }
}
