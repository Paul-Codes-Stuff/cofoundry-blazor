using Cofoundry.Web;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Restricts a component to signed-in users of the Cofoundry admin user area. This is the
/// Blazor admin's equivalent of <c>AdminAuthorizeAttribute</c> in the AngularJS admin project,
/// duplicated here so the two projects stay independent.
/// </summary>
public class AdminBlazorAuthorizeAttribute : AuthorizeUserAreaAttribute
{
    public AdminBlazorAuthorizeAttribute()
        : base(CofoundryAdminUserArea.Code)
    {
    }
}
