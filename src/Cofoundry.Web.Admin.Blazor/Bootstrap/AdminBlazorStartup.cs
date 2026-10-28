using System.Reflection;
using Cofoundry.Web.Internal;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        mvcBuilder.Services
            .AddOptions<MvcOptions>()
            .PostConfigure<AdminSettings, PagesSettings, AdminBlazorSettings>(VisualEditorInstallation.ConfigureFilters);

        // The requirement is the admin's own rather than Cofoundry's UserAreaAuthorizationRequirement, so that
        // AdminBlazorAuthorizationHandler checks the user in its own scope (see its remarks).
        mvcBuilder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminBlazorAuthorizeAttribute.PolicyName, policy => policy
                .AddAuthenticationSchemes(AuthenticationSchemeNames.UserArea(CofoundryAdminUserArea.Code))
                .AddRequirements(new AdminBlazorAuthorizationRequirement()));
        });
        mvcBuilder.Services.AddTransient<IAuthorizationHandler, AdminBlazorAuthorizationHandler>();

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
/// Maps the Blazor admin in its own branch of the pipeline under <see cref="AdminBlazorSettings.DirectoryName"/>,
/// before Cofoundry's routing, so Cofoundry's catch-all page route never sees its requests.
/// </summary>
/// <remarks>
/// Razor <c>@page</c> routes are literals, so pages declare them without the prefix. In the branch the prefix
/// is the request's <c>PathBase</c> while routing, which is how ASP.NET Core hosts an app under a sub-path:
/// the endpoints match the declared routes, so static SSR finds its page in the router's table of declared
/// routes, and App.razor's base href gives the browser the same base. A route group can't do this: static
/// SSR looks the endpoint's prefixed template up among the declared routes, and fails.
/// </remarks>
public class AdminBlazorStartupConfigurationTask
    : IStartupConfigurationTask
    , IRunAfterStartupConfigurationTask
    , IRunBeforeStartupConfigurationTask
{
    private readonly AdminSettings _adminSettings;
    private readonly AdminBlazorSettings _adminBlazorSettings;
    private readonly AdminBlazorModuleRegistry _moduleRegistry;
    private readonly IEnumerable<ICustomEntityDefinition> _customEntityDefinitions;

    public AdminBlazorStartupConfigurationTask(
        AdminSettings adminSettings,
        AdminBlazorSettings adminBlazorSettings,
        AdminBlazorModuleRegistry moduleRegistry,
        IEnumerable<ICustomEntityDefinition> customEntityDefinitions
        )
    {
        _adminSettings = adminSettings;
        _adminBlazorSettings = adminBlazorSettings;
        _moduleRegistry = moduleRegistry;
        _customEntityDefinitions = customEntityDefinitions;
    }

    public int Ordering => (int)StartupTaskOrdering.Early;

    // After static files (blazor.web.js and the _content assets are served from the site root) and the
    // database auto-update.
    public IReadOnlyCollection<Type> RunAfter => [typeof(StaticFileStartupConfigurationTask), typeof(AutoUpdateMiddlewareStartupConfigurationTask)];

    public IReadOnlyCollection<Type> RunBefore => [typeof(UseRoutingStartupConfigurationTask)];

    public void Configure(IApplicationBuilder app)
    {
        // Before anything reads the routes.
        AdminBlazorRoutes.SetPrefix(_adminBlazorSettings.DirectoryName);
        AdminBlazorRoutes.SetCustomEntitySlugs(_customEntityDefinitions, GetReservedRouteSegments());

        if (_adminSettings.Disabled)
        {
            return;
        }

        // "{Root}/api" is left to the main pipeline, for Cofoundry.Web.Admin.Api (Cofoundry:AdminApi:BaseUrl),
        // e.g. the Blazor admin at "admin" and the API at its default "admin/api". No admin page uses it.
        app.MapWhen(IsAdminRequest, admin =>
        {
            // What app.Map does: the prefix moves from the Path to the PathBase.
            admin.Use((context, next) =>
            {
                var request = context.Request;
                request.Path.StartsWithSegments(AdminBlazorRoutes.Root, StringComparison.OrdinalIgnoreCase, out var matched, out var remaining);
                request.PathBase = request.PathBase.Add(matched);
                request.Path = remaining;

                return next(context);
            });

            admin.UseRouting();

            // The prefix only needs to be the PathBase for matching. Put the request back as it came, since
            // Cofoundry builds absolute urls from the PathBase (ISiteUrlResolver), which would otherwise
            // double the prefix in, e.g., the emails the admin sends. The router uses the matched endpoint.
            admin.Use((context, next) =>
            {
                var request = context.Request;
                if (request.PathBase.Value?.EndsWith(AdminBlazorRoutes.Root, StringComparison.OrdinalIgnoreCase) == true)
                {
                    var pathBase = request.PathBase.Value;
                    request.Path = new PathString(pathBase[^AdminBlazorRoutes.Root.Length..]).Add(request.Path);
                    request.PathBase = new PathString(pathBase[..^AdminBlazorRoutes.Root.Length]);
                }

                return next(context);
            });

            admin.UseAuthentication();
            admin.UseAuthorization();

            // Razor Component endpoints carry anti-forgery metadata, checked between routing and the endpoint.
            admin.UseAntiforgery();

            admin.UseEndpoints(endpoints =>
            {
                // Pages from other assemblies (IAdminBlazorModuleRegistration) need endpoints too, or a
                // request straight to one 404s. Routes.razor adds them to the router.
                endpoints
                    .MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode()
                    .AddAdditionalAssemblies([.. _moduleRegistry.AdditionalAssemblies]);

                // Authenticating runs the cookie's checks (expiry, and the security stamp once its validation
                // interval has passed). A plain 401 rather than a challenge, which would redirect to sign in.
                endpoints.MapGet(AdminBlazorRoutes.SessionCheckRoute, async (HttpContext context) =>
                {
                    var result = await context.AuthenticateAsync(AuthenticationSchemeNames.UserArea(CofoundryAdminUserArea.Code));
                    var isSignedIn = result.Succeeded
                        && result.Principal.FindFirst(CofoundryClaimTypes.UserAreaCode)?.Value == CofoundryAdminUserArea.Code;

                    return isSignedIn ? Results.NoContent() : Results.Unauthorized();
                });
            });
        });
    }

    /// <summary>
    /// The first segments of the admin's routes, which a custom entity's url can't use: every page's (those
    /// starting with a literal), and the endpoints' that aren't pages.
    /// </summary>
    private IEnumerable<string> GetReservedRouteSegments()
    {
        var pageSegments = _moduleRegistry.AdditionalAssemblies
            .Prepend(typeof(App).Assembly)
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => typeof(Microsoft.AspNetCore.Components.IComponent).IsAssignableFrom(t))
            .SelectMany(t => t.GetCustomAttributes<Microsoft.AspNetCore.Components.RouteAttribute>())
            .Select(r => r.Template.Trim('/').Split('/')[0])
            .Where(s => s.Length > 0 && !s.StartsWith('{'));

        return pageSegments.Concat([AdminBlazorRoutes.ApiRoute.Trim('/'), "_blazor"]);
    }

    private static bool IsAdminRequest(HttpContext context)
    {
        var path = context.Request.Path;

        return path.StartsWithSegments(AdminBlazorRoutes.Root, StringComparison.OrdinalIgnoreCase, out var remaining)
            && !remaining.StartsWithSegments(AdminBlazorRoutes.ApiRoute, StringComparison.OrdinalIgnoreCase);
    }
}
