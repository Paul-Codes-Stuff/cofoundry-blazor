namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Told about each unexpected exception that the admin's error boundaries (<c>AdminErrorBoundary</c>: pages,
/// dialogs and the visual editor's frame) catch, after it has been logged with <c>ILogger</c>. These exceptions
/// happen in a Blazor circuit, not in an HTTP request, so request middleware (e.g. the Error Logging plugin's)
/// never sees them: implement this to record them. Implementations are found by assembly scanning and resolved
/// from the circuit's scope, so use <see cref="IAdminBlazorExecutor"/> for anything that needs the database.
/// </summary>
public interface IAdminBlazorErrorHandler
{
    /// <summary>
    /// Handles an exception the admin has already shown an error message for. An exception thrown from here is
    /// logged and otherwise ignored.
    /// </summary>
    Task HandleAsync(Exception exception);
}
