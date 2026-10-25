namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Central definition of the routes used by the Blazor admin. Blazor <c>@page</c> routes
/// are compile-time constants, so the prefix cannot be driven from configuration like the
/// AngularJS admin's <c>AdminSettings.DirectoryName</c>. It is deliberately different to the
/// AngularJS admin prefix so that both admins can be installed side-by-side during migration.
/// Razor <c>@page</c> directives only accept string literals, so each page component repeats the
/// "admin-next" prefix literally. To change the prefix, update <see cref="Prefix"/> and every
/// <c>@page</c> directive (search for "admin-next").
/// </summary>
public static class AdminBlazorRoutes
{
    /// <summary>
    /// The root path segment of the Blazor admin, without slashes.
    /// </summary>
    public const string Prefix = "admin-next";

    public const string Root = "/" + Prefix;

    public const string Pages = Root + "/pages";
    public const string PageNew = Pages + "/new";
    public const string PageDetailsTemplate = Pages + "/{PageId:int}";

    public static string PageDetails(int pageId) => $"{Pages}/{pageId}";

    /// <summary>
    /// Authentication currently stays with the AngularJS admin's MVC auth controller,
    /// which is shared during the transition. <paramref name="adminDirectoryName"/> is
    /// the <see cref="AdminSettings.DirectoryName"/> of that admin.
    /// </summary>
    public static string Login(string adminDirectoryName, string? returnUrl = null)
    {
        var url = $"/{adminDirectoryName}/auth/login";
        if (!string.IsNullOrEmpty(returnUrl))
        {
            url += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
        }
        return url;
    }

    public static string Logout(string adminDirectoryName) => $"/{adminDirectoryName}/auth/logout";
}
