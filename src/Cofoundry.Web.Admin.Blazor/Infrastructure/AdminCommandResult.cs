namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// The outcome of running a command through <see cref="IAdminBlazorExecutor"/>. Validation
/// and permission failures are returned as errors rather than thrown so components can
/// display them in the form status area.
/// </summary>
public sealed class AdminCommandResult
{
    public static readonly AdminCommandResult Success = new([]);

    public AdminCommandResult(IReadOnlyCollection<ValidationError> errors)
    {
        Errors = errors;
    }

    public IReadOnlyCollection<ValidationError> Errors { get; }

    public bool Succeeded => Errors.Count == 0;
}
