namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Central definition of the routes used by the Blazor admin. The prefix comes from
/// <see cref="AdminBlazorSettings.DirectoryName"/> ("admin-next" by default), which must differ from the
/// AngularJS admin's <c>AdminSettings.DirectoryName</c> while both admins are installed.
/// <para>
/// Razor <c>@page</c> directives only accept string literals, so pages declare their routes without the
/// prefix (e.g. <c>@page "/pages"</c>). <c>AdminBlazorStartupConfigurationTask</c> maps them in a branch of
/// the pipeline for the prefix, and <c>App.razor</c> sets the document's base href to it.
/// </para>
/// </summary>
public static class AdminBlazorRoutes
{
    public const string DefaultPrefix = "admin-next";

    /// <summary>
    /// The root path segment of the Blazor admin, without slashes. Set from
    /// <see cref="AdminBlazorSettings.DirectoryName"/> when the routes are registered.
    /// </summary>
    public static string Prefix { get; private set; } = DefaultPrefix;

    internal static void SetPrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        Prefix = prefix.Trim('/');
    }

    public static string Root => "/" + Prefix;

    /// <summary>
    /// The landing page. <see cref="Root"/> also renders the dashboard.
    /// </summary>
    public static string Dashboard => Root + "/dashboard";

    public static string Pages => Root + "/pages";

    public static string PagesInDirectory(int pageDirectoryId) => $"{Pages}?pageDirectoryId={pageDirectoryId}";

    public static string PagesUsingTemplate(int pageTemplateId) => $"{Pages}?pageTemplateId={pageTemplateId}";
    public static string PageNew => Pages + "/new";

    public static string PageDetails(int pageId) => $"{Pages}/{pageId}";

    public static string Images => Root + "/images";
    public static string ImageNew => Images + "/new";

    public static string ImageDetails(int imageAssetId) => $"{Images}/{imageAssetId}";

    public static string Documents => Root + "/documents";
    public static string DocumentNew => Documents + "/new";

    public static string DocumentDetails(int documentAssetId) => $"{Documents}/{documentAssetId}";

    public static string Settings => Root + "/settings";

    public static string Roles => Root + "/roles";
    public static string RoleNew => Roles + "/new";

    public static string RoleDetails(int roleId) => $"{Roles}/{roleId}";

    public static string PageTemplates => Root + "/page-templates";

    public static string PageTemplateDetails(int pageTemplateId) => $"{PageTemplates}/{pageTemplateId}";

    public static string Directories => Root + "/directories";
    public static string DirectoryNew => Directories + "/new";

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
    public static string Account => Root + "/account";
    public static string AccountEdit => Account + "?edit=true";
    public static string AccountChangePassword => Account + "/change-password";

    /// <summary>
    /// First-run setup, only available until the site is set up.
    /// </summary>
    public static string Setup => Root + "/setup";

    public static string Login => Root + "/auth/login";
    public static string Logout => Root + "/auth/logout";
    public static string ForgotPassword => Root + "/auth/forgot-password";

    /// <summary>
    /// Returns 204 while the admin is signed in, else 401. Polled by <c>js/session-check.js</c>.
    /// </summary>
    public static string SessionCheck => Root + SessionCheckRoute;

    /// <summary>
    /// <see cref="SessionCheck"/>'s route within the admin, where the prefix is the request's PathBase.
    /// </summary>
    public const string SessionCheckRoute = "/auth/session";

    /// <summary>
    /// Reserved within the admin for Cofoundry.Web.Admin.Api, whose default base url is the admin directory
    /// followed by "/api": the admin doesn't handle requests under it.
    /// </summary>
    public const string ApiRoute = "/api";

    /// <summary>
    /// The account recovery page linked from the reset password email, which adds the token as a
    /// query parameter (see <see cref="AdminBlazorMailTemplateBuilder"/>).
    /// </summary>
    public static string ResetPassword => Root + "/auth/reset-password";

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
