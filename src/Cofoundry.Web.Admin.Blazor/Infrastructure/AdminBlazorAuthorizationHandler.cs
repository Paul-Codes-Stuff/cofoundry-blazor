using Cofoundry.Web.Internal;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// The requirement of the <see cref="AdminBlazorAuthorizeAttribute.PolicyName"/> policy: a signed-in user of the
/// Cofoundry admin user area. Handled by <see cref="AdminBlazorAuthorizationHandler"/>.
/// </summary>
public class AdminBlazorAuthorizationRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Checks the same as Cofoundry's <c>UserAreaAuthorizationHandler</c> for the admin user area (the user exists, is
/// active and belongs to the area), but in its own DI scope seeded with the user id from the principal, as
/// <see cref="AdminBlazorExecutor"/> does.
/// </summary>
/// <remarks>
/// Cofoundry's handler queries through the scope it's resolved from. In a circuit that's the circuit's scope,
/// which Blazor disposes, with its shared database connection, as soon as the tab navigates away. A tab that
/// leaves while its <c>AuthorizeRouteView</c> is still checking (a quick click, or a test navigating on) failed
/// the query with "The connection is closed" and logged an unhandled circuit exception. A scope owned by the
/// check lives until the check is done. It also means the circuit no longer caches the user for its lifetime,
/// so a deactivated user is refused on their next page.
/// </remarks>
public class AdminBlazorAuthorizationHandler : AuthorizationHandler<AdminBlazorAuthorizationRequirement>
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public AdminBlazorAuthorizationHandler(
        IServiceScopeFactory serviceScopeFactory
        )
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminBlazorAuthorizationRequirement requirement)
    {
        var userId = GetAdminUserId(context.User);
        if (!userId.HasValue)
        {
            return;
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<AdminBlazorScopeUser>().Set(userId.Value);

        var userContextService = scope.ServiceProvider.GetRequiredService<IUserContextService>();
        var userContext = await userContextService.GetCurrentContextAsync();
        var signedInUser = userContext.ToSignedInContext();

        if (signedInUser != null && signedInUser.UserArea.UserAreaCode == CofoundryAdminUserArea.Code)
        {
            context.Succeed(requirement);
        }
    }

    /// <summary>
    /// The user id of a principal signed in to the Cofoundry admin user area, or <see langword="null"/>.
    /// </summary>
    public static int? GetAdminUserId(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var areaCode = user.FindFirst(CofoundryClaimTypes.UserAreaCode)?.Value;
        if (areaCode != CofoundryAdminUserArea.Code)
        {
            return null;
        }

        var userIdClaim = user.FindFirst(CofoundryClaimTypes.UserId)?.Value;

        return int.TryParse(userIdClaim, out var userId) && userId > 0 ? userId : null;
    }
}
