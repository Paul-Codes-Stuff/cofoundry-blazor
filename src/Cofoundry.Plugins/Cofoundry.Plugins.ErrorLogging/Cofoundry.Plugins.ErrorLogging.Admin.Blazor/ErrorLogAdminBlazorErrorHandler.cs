using Cofoundry.Plugins.ErrorLogging.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Plugins.ErrorLogging.Admin.Blazor;

/// <summary>
/// Adds the Blazor admin's unexpected exceptions to the error log. They happen in a Blazor circuit, so
/// <see cref="ErrorLoggingMiddleware"/> never sees them. Logs from a scope seeded with the signed-in admin user,
/// whom the error email names.
/// </summary>
public class ErrorLogAdminBlazorErrorHandler : IAdminBlazorErrorHandler
{
    private readonly IAdminBlazorExecutor _executor;

    public ErrorLogAdminBlazorErrorHandler(
        IAdminBlazorExecutor executor
        )
    {
        _executor = executor;
    }

    public Task HandleAsync(Exception exception)
    {
        return _executor.WithServicesAsync(async services =>
        {
            await services.GetRequiredService<IErrorLoggingService>().LogAsync(exception);

            return true;
        });
    }
}
