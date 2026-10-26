namespace Cofoundry.Web.Admin.Api;

public static class AdminApiRouteConstants
{
    /// <summary>
    /// The MVC area of the API controllers. It differs from Cofoundry.Web.Admin's "CofoundryAdmin" area, so
    /// that both copies of a controller (e.g. "PagesApi") can be installed and each route reaches its own.
    /// </summary>
    public const string AreaName = "CofoundryAdminApi";
}
