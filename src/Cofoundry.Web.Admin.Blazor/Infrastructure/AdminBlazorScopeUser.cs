namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Scoped holder for the admin user a short-lived DI scope is executing on behalf of.
/// <para>
/// Blazor Server event handlers run over SignalR where <c>HttpContext</c> is not reliably
/// available, but Cofoundry's <c>WebUserSessionService</c> resolves the current user from
/// <c>HttpContext</c>. <see cref="AdminBlazorExecutor"/> creates a fresh scope per operation,
/// seeds this holder from the circuit's authenticated principal, and
/// <see cref="CircuitAwareUserSessionService"/> then serves that user to the domain layer.
/// </para>
/// </summary>
public class AdminBlazorScopeUser
{
    public int? UserId { get; private set; }

    public bool HasUser => UserId.HasValue;

    public void Set(int userId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(userId, 1);
        UserId = userId;
    }
}
