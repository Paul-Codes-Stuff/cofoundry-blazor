using Cofoundry.Web.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// An <see cref="IUserSessionService"/> that serves the user held in
/// <see cref="AdminBlazorScopeUser"/> when one has been seeded (i.e. inside a Blazor circuit
/// operation), and otherwise defers to Cofoundry's standard <see cref="WebUserSessionService"/>
/// so normal MVC requests are completely unaffected.
/// </summary>
public class CircuitAwareUserSessionService : IUserSessionService
{
    private readonly AdminBlazorScopeUser _scopeUser;
    private readonly Lazy<IUserSessionService> _webSessionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CircuitAwareUserSessionService> _logger;

    public CircuitAwareUserSessionService(
        AdminBlazorScopeUser scopeUser,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CircuitAwareUserSessionService> logger,
        IServiceProvider serviceProvider
        )
    {
        _scopeUser = scopeUser;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _webSessionService = new Lazy<IUserSessionService>(
            () => ActivatorUtilities.CreateInstance<WebUserSessionService>(serviceProvider)
            );
    }

    public int? GetCurrentUserId()
    {
        if (_scopeUser.HasUser)
        {
            return _scopeUser.UserId;
        }

        return _webSessionService.Value.GetCurrentUserId();
    }

    public Task<int?> GetUserIdByUserAreaCodeAsync(string userAreaCode)
    {
        ArgumentNullException.ThrowIfNull(userAreaCode);

        // The scope user is only ever seeded from a signed-in admin (COF) principal.
        if (_scopeUser.HasUser && userAreaCode == CofoundryAdminUserArea.Code)
        {
            return Task.FromResult(_scopeUser.UserId);
        }

        return _webSessionService.Value.GetUserIdByUserAreaCodeAsync(userAreaCode);
    }

    // Everything below mutates the cookie session and so requires a real HttpContext;
    // sign in and out are not used from within a circuit, so we simply defer to the web implementation.

    public Task SignInAsync(string userAreaDefinitionCode, int userId, bool rememberUser)
        => _webSessionService.Value.SignInAsync(userAreaDefinitionCode, userId, rememberUser);

    public Task SignOutAsync(string userAreaDefinitionCode)
        => _webSessionService.Value.SignOutAsync(userAreaDefinitionCode);

    public Task SignOutOfAllUserAreasAsync()
        => _webSessionService.Value.SignOutOfAllUserAreasAsync();

    public Task SetAmbientUserAreaAsync(string userAreaCode)
        => _scopeUser.HasUser
            ? Task.CompletedTask
            : _webSessionService.Value.SetAmbientUserAreaAsync(userAreaCode);

    /// <summary>
    /// The domain calls this after a command updates a user's security stamp, to re-issue the cookie of
    /// a user who changed their own. A circuit's response started long ago, so it can't set a cookie and
    /// the web implementation would throw after the change was saved. That's skipped here: pages where
    /// you change your own stamp (My Account) are static SSR, and the users screens don't let you edit
    /// those fields on yourself. A static SSR request still re-issues the cookie as normal.
    /// </summary>
    public Task RefreshAsync(string userAreaCode, int userId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (_scopeUser.HasUser && (httpContext == null || httpContext.Response.HasStarted))
        {
            _logger.LogDebug("Skipped refreshing the sign in of user {UserId} in a Blazor circuit.", userId);
            return Task.CompletedTask;
        }

        return _webSessionService.Value.RefreshAsync(userAreaCode, userId);
    }
}
