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

    /// <summary>
    /// The landing page. <see cref="Root"/> also renders the dashboard.
    /// </summary>
    public const string Dashboard = Root + "/dashboard";

    public const string Pages = Root + "/pages";
    public const string PageNew = Pages + "/new";
    public const string PageDetailsTemplate = Pages + "/{PageId:int}";

    public static string PageDetails(int pageId) => $"{Pages}/{pageId}";

    public const string Images = Root + "/images";
    public const string ImageNew = Images + "/new";

    public static string ImageDetails(int imageAssetId) => $"{Images}/{imageAssetId}";

    public const string Documents = Root + "/documents";
    public const string DocumentNew = Documents + "/new";

    public static string DocumentDetails(int documentAssetId) => $"{Documents}/{documentAssetId}";

    public const string Login = Root + "/auth/login";
    public const string Logout = Root + "/auth/logout";

    public static string LoginWithReturnUrl(string? returnUrl)
    {
        return string.IsNullOrEmpty(returnUrl) ? Login : $"{Login}?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
    }

    /// <summary>
    /// True if <paramref name="url"/> is a path on this site, so it is safe to redirect to after
    /// sign-in. Rejects absolute and protocol-relative urls ("//host", "/\host").
    /// </summary>
    public static bool IsLocalUrl(string? url)
    {
        return !string.IsNullOrEmpty(url)
            && url[0] == '/'
            && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }

    /// <summary>
    /// Pages that still live in the AngularJS admin's MVC controllers. Each takes the
    /// <see cref="AdminSettings.DirectoryName"/> of that admin.
    /// </summary>
    public static class Mvc
    {
        public static string Setup(string adminDirectoryName) => $"/{adminDirectoryName}/setup";

        public static string ForgotPassword(string adminDirectoryName) => $"/{adminDirectoryName}/auth/forgot-password";

        public static string ChangePassword(string adminDirectoryName, string? returnUrl)
        {
            var url = $"/{adminDirectoryName}/auth/change-password";
            return string.IsNullOrEmpty(returnUrl) ? url : $"{url}?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        }
    }
}
