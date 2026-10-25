using System.Globalization;

namespace Cofoundry.Plugins.ErrorLogging.Admin.Blazor;

/// <summary>
/// The error log module's Blazor urls. The <c>@page</c> directives repeat them, since they only accept
/// literals.
/// </summary>
public static class ErrorsBlazorRoutes
{
    public const string List = AdminBlazorRoutes.Root + "/errors";

    public static string Details(int errorId) => List + "/" + errorId.ToString(CultureInfo.InvariantCulture);
}
