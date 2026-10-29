using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Cofoundry.Web.Admin.Blazor.Components.Layout;

/// <summary>
/// The admin's <see cref="ErrorBoundary"/>: logs the exception as the base class does, then passes it to each
/// <see cref="IAdminBlazorErrorHandler"/>, e.g. the Error Logging plugin's.
/// </summary>
public class AdminErrorBoundary : ErrorBoundary
{
    [Inject] private IEnumerable<IAdminBlazorErrorHandler> ErrorHandlers { get; set; } = default!;

    [Inject] private ILogger<AdminErrorBoundary> Logger { get; set; } = default!;

    protected override async Task OnErrorAsync(Exception exception)
    {
        await base.OnErrorAsync(exception);

        foreach (var handler in ErrorHandlers)
        {
            // An exception from here would end the circuit.
            try
            {
                await handler.HandleAsync(exception);
            }
            catch (Exception handlerException)
            {
                Logger.LogError(handlerException, "Error handler {HandlerType} failed handling {ExceptionType}", handler.GetType().FullName, exception.GetType().FullName);
            }
        }
    }
}
