using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Api;

/// <summary>
/// <see cref="IEndpointRouteBuilder"/> extensions for adding admin API routes.
/// </summary>
public static class AdminApiRouteBuilderExtensions
{
    extension(IEndpointRouteBuilder routeBuilder)
    {
        /// <summary>
        /// Returns a builder context than can be used to add admin api routes to the
        /// specified controller. Routes specified through this method will automatically
        /// be registered under the admin api path, which can be altered via the
        /// Cofoundry:AdminApi:BaseUrl setting.
        /// </summary>
        /// <typeparam name="TController">
        /// The controller type to add routes for. This type is used to scope the
        /// api routes to the controller. It should inherit from <see cref="BaseAdminApiController"/>,
        /// which puts it in the <see cref="AdminApiRouteConstants.AreaName"/> area.
        /// </typeparam>
        /// <param name="basePath">
        /// The base path of all routes on the controller, without the admin api directory
        /// or any leading or trailing slashes e.g. "auth", "images", "users/roles"
        /// </param>
        /// <returns>Api builder context that can be used to add routes.</returns>
        public AdminApiRouteBuilderContext<TController> ForAdminApiController<TController>(string basePath)
            where TController : ControllerBase
        {
            var adminSettings = routeBuilder.ServiceProvider.GetRequiredService<AdminSettings>();
            var adminApiSettings = routeBuilder.ServiceProvider.GetRequiredService<AdminApiSettings>();
            return new AdminApiRouteBuilderContext<TController>(adminSettings, adminApiSettings, routeBuilder, basePath);
        }
    }
}
