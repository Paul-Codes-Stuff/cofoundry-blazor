using Cofoundry.Web.Auth.Internal;
using Microsoft.AspNetCore.Authentication.Cookies;
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

        mvcBuilder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminBlazorAuthorizeAttribute.PolicyName, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemeNames.UserArea(CofoundryAdminUserArea.Code))
                .AddRequirements(new UserAreaAuthorizationRequirement(CofoundryAdminUserArea.Code)));
        });

        // The admin cookie scheme challenges to the AngularJS admin's login page. Send requests for the
        // Blazor admin to its own login instead, keeping the original handler (which also handles ajax).
        mvcBuilder.Services.PostConfigure<CookieAuthenticationOptions>(
            AuthenticationSchemeNames.UserArea(CofoundryAdminUserArea.Code),
            options =>
            {
                var redirectToLogin = options.Events.OnRedirectToLogin;
                options.Events.OnRedirectToLogin = context =>
                {
                    var request = context.Request;
                    if (request.Path.StartsWithSegments(AdminBlazorRoutes.Root, StringComparison.OrdinalIgnoreCase))
                    {
                        context.RedirectUri = AdminBlazorRoutes.LoginWithReturnUrl(request.PathBase + request.Path + request.QueryString);
                    }

                    return redirectToLogin(context);
                };
            });
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
    private readonly AdminBlazorModuleRegistry _moduleRegistry;

    public AdminBlazorRouteRegistration(AdminSettings adminSettings, AdminBlazorModuleRegistry moduleRegistry)
    {
        _adminSettings = adminSettings;
        _moduleRegistry = moduleRegistry;
    }

    public int Ordering => (int)RouteRegistrationOrdering.Early;

    public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
    {
        if (_adminSettings.Disabled)
        {
            return;
        }

        // Pages from other assemblies (IAdminBlazorModuleRegistration) need endpoints too, or a request
        // straight to one falls through to Cofoundry's page route. Routes.razor adds them to the router.
        routeBuilder
            .MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddAdditionalAssemblies([.. _moduleRegistry.AdditionalAssemblies]);
    }
}
