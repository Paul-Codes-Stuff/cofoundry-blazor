using System.Globalization;

namespace Cofoundry.Plugins.ErrorLogging.Admin.Blazor;

/// <summary>
/// The error log module's Blazor urls. The <c>@page</c> directives repeat them without the admin's prefix,
/// since they only accept literals (see <see cref="AdminBlazorRoutes"/>).
/// </summary>
public static class ErrorsBlazorRoutes
{
    public static string List => AdminBlazorRoutes.Root + "/errors";

    public static string Details(int errorId) => List + "/" + errorId.ToString(CultureInfo.InvariantCulture);
}
