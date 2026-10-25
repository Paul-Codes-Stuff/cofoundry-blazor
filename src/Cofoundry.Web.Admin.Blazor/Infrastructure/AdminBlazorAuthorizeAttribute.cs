using Cofoundry.Web;
using Microsoft.AspNetCore.Authorization;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Restricts a component to signed-in users of the Cofoundry admin user area. This is the
/// Blazor admin's equivalent of <c>AdminAuthorizeAttribute</c> in the AngularJS admin project.
/// </summary>
/// <remarks>
/// Unlike <see cref="AuthorizeUserAreaAttribute"/>, this does not set <see cref="AuthorizeAttribute.AuthenticationSchemes"/>:
/// <c>AuthorizeRouteView</c> throws if a component specifies a scheme. The admin scheme is carried
/// by the <see cref="PolicyName"/> policy instead, which endpoint authorization still honours.
/// </remarks>
public class AdminBlazorAuthorizeAttribute : AuthorizeAttribute
{
    public const string PolicyName = "Cofoundry_AdminBlazor";

    public AdminBlazorAuthorizeAttribute()
        : base(PolicyName)
    {
    }
}
