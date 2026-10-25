using Cofoundry.Web.Internal;
using Microsoft.Extensions.DependencyInjection;

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

    public CircuitAwareUserSessionService(
        AdminBlazorScopeUser scopeUser,
        IServiceProvider serviceProvider
        )
    {
        _scopeUser = scopeUser;
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
    // it is not used from within a circuit, so we simply defer to the web implementation.

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

    public Task RefreshAsync(string userAreaCode, int userId)
        => _webSessionService.Value.RefreshAsync(userAreaCode, userId);
}
