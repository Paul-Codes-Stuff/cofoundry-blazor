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

    public static string PagesInDirectory(int pageDirectoryId) => $"{Pages}?pageDirectoryId={pageDirectoryId}";

    public static string PagesUsingTemplate(int pageTemplateId) => $"{Pages}?pageTemplateId={pageTemplateId}";
    public const string PageNew = Pages + "/new";
    public const string PageDetailsTemplate = Pages + "/{PageId:int}";

    public static string PageDetails(int pageId) => $"{Pages}/{pageId}";

    public const string Images = Root + "/images";
    public const string ImageNew = Images + "/new";

    public static string ImageDetails(int imageAssetId) => $"{Images}/{imageAssetId}";

    public const string Documents = Root + "/documents";
    public const string DocumentNew = Documents + "/new";

    public static string DocumentDetails(int documentAssetId) => $"{Documents}/{documentAssetId}";

    public const string Settings = Root + "/settings";

    public const string Roles = Root + "/roles";
    public const string RoleNew = Roles + "/new";

    public static string RoleDetails(int roleId) => $"{Roles}/{roleId}";

    public const string PageTemplates = Root + "/page-templates";

    public static string PageTemplateDetails(int pageTemplateId) => $"{PageTemplates}/{pageTemplateId}";

    public const string Directories = Root + "/directories";
    public const string DirectoryNew = Directories + "/new";

    public static string DirectoryDetails(int pageDirectoryId) => $"{Directories}/{pageDirectoryId}";

    /// <summary>
    /// Each custom entity definition is its own module, keyed by its 6 character definition code,
    /// e.g. "/admin-next/custom-entities/SIMBLP".
    /// </summary>
    public static string CustomEntities(string customEntityDefinitionCode) => $"{Root}/custom-entities/{customEntityDefinitionCode}";

    public static string CustomEntityNew(string customEntityDefinitionCode) => CustomEntities(customEntityDefinitionCode) + "/new";

    public static string CustomEntityDetails(string customEntityDefinitionCode, int customEntityId) =>
        $"{CustomEntities(customEntityDefinitionCode)}/{customEntityId}";

    /// <summary>
    /// Users are listed per user area, keyed by its 3 character code, e.g. "/admin-next/users/COF".
    /// </summary>
    public static string Users(string userAreaCode) => $"{Root}/users/{userAreaCode}";

    public static string UserNew(string userAreaCode) => Users(userAreaCode) + "/new";

    public static string UserDetails(string userAreaCode, int userId) => $"{Users(userAreaCode)}/{userId}";

    /// <summary>
    /// The static SSR form for editing yourself, see <c>UserEditSelfPage</c>.
    /// </summary>
    public static string UserEditSelf(string userAreaCode, int userId) => UserDetails(userAreaCode, userId) + "/edit";

    /// <summary>
    /// The signed-in user's own details ("My Account").
    /// </summary>
    public const string Account = Root + "/account";
    public const string AccountEdit = Account + "?edit=true";
    public const string AccountChangePassword = Account + "/change-password";

    /// <summary>
    /// First-run setup, only available until the site is set up.
    /// </summary>
    public const string Setup = Root + "/setup";

    public const string Login = Root + "/auth/login";
    public const string Logout = Root + "/auth/logout";
    public const string ForgotPassword = Root + "/auth/forgot-password";

    /// <summary>
    /// Returns 204 while the admin is signed in, else 401. Polled by <c>js/session-check.js</c>.
    /// </summary>
    public const string SessionCheck = Root + "/auth/session";

    /// <summary>
    /// The account recovery page linked from the reset password email, which adds the token as a
    /// query parameter (see <see cref="AdminBlazorMailTemplateBuilder"/>).
    /// </summary>
    public const string ResetPassword = Root + "/auth/reset-password";

    public static string ForgotPasswordWithEmail(string? email)
    {
        return string.IsNullOrEmpty(email) ? ForgotPassword : $"{ForgotPassword}?email={Uri.EscapeDataString(email)}";
    }

    /// <summary>
    /// Where a user who must change their password is sent from sign in.
    /// </summary>
    public static string ChangePassword(string? returnUrl)
    {
        var url = Root + "/auth/change-password";
        return string.IsNullOrEmpty(returnUrl) ? url : $"{url}?ReturnUrl={Uri.EscapeDataString(returnUrl)}";
    }

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
}
