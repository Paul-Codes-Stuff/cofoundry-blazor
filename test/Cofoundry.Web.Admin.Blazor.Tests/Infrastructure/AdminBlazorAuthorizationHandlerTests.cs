using System.Security.Claims;
using Cofoundry.Domain;
using Cofoundry.Web.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

public class AdminBlazorAuthorizationHandlerTests
{
    private const int ActiveAdminId = 5;
    private const int DeactivatedAdminId = 6;
    private const int MemberAreaUserId = 7;

    [Fact]
    public async Task ActiveAdmin_Succeeds_CheckedInItsOwnSeededScope()
    {
        var (handler, userContexts) = CreateHandler();

        var context = await AuthorizeAsync(handler, AdminPrincipal(ActiveAdminId));

        Assert.True(context.HasSucceeded);
        Assert.Equal([ActiveAdminId], userContexts.SeenUserIds);
        Assert.Equal(1, userContexts.DisposedScopes);
    }

    [Fact]
    public async Task UserTheDatabaseNoLongerSignsIn_Fails()
    {
        var (handler, _) = CreateHandler();

        var context = await AuthorizeAsync(handler, AdminPrincipal(DeactivatedAdminId));

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task UserTheDatabasePutsInAnotherArea_Fails()
    {
        var (handler, _) = CreateHandler();

        var context = await AuthorizeAsync(handler, AdminPrincipal(MemberAreaUserId));

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task AnonymousOrOtherAreaPrincipal_FailsWithoutQuerying()
    {
        var (handler, userContexts) = CreateHandler();

        var anonymous = await AuthorizeAsync(handler, new ClaimsPrincipal(new ClaimsIdentity()));
        var member = await AuthorizeAsync(handler, Principal("MEM", ActiveAdminId.ToString()));

        Assert.False(anonymous.HasSucceeded);
        Assert.False(member.HasSucceeded);
        Assert.Empty(userContexts.SeenUserIds);
    }

    [Theory]
    [InlineData("COF", "12", 12)]
    [InlineData("COF", "0", null)]
    [InlineData("COF", "abc", null)]
    [InlineData("MEM", "12", null)]
    [InlineData(null, "12", null)]
    public void GetAdminUserId_ReadsTheAdminClaims(string? areaCode, string userId, int? expected)
    {
        Assert.Equal(expected, AdminBlazorAuthorizationHandler.GetAdminUserId(Principal(areaCode, userId)));
    }

    private static async Task<AuthorizationHandlerContext> AuthorizeAsync(AdminBlazorAuthorizationHandler handler, ClaimsPrincipal user)
    {
        var context = new AuthorizationHandlerContext([new AdminBlazorAuthorizationRequirement()], user, null);
        await handler.HandleAsync(context);

        return context;
    }

    private static ClaimsPrincipal AdminPrincipal(int userId) => Principal(CofoundryAdminUserArea.Code, userId.ToString());

    private static ClaimsPrincipal Principal(string? areaCode, string userId)
    {
        var claims = new List<Claim> { new(CofoundryClaimTypes.UserId, userId) };
        if (areaCode != null)
        {
            claims.Add(new(CofoundryClaimTypes.UserAreaCode, areaCode));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static (AdminBlazorAuthorizationHandler Handler, UserContextLog Log) CreateHandler()
    {
        var log = new UserContextLog();
        var services = new ServiceCollection()
            .AddSingleton(log)
            .AddScoped<AdminBlazorScopeUser>()
            .AddScoped<IUserContextService, FakeUserContextService>()
            .BuildServiceProvider();

        return (new AdminBlazorAuthorizationHandler(services.GetRequiredService<IServiceScopeFactory>()), log);
    }

    private sealed class UserContextLog
    {
        public List<int?> SeenUserIds { get; } = [];

        public int DisposedScopes { get; set; }
    }

    /// <summary>
    /// Stands in for the database lookup: reads the user id seeded in its scope, as Cofoundry's service does
    /// through the session service.
    /// </summary>
    private sealed class FakeUserContextService(AdminBlazorScopeUser scopeUser, UserContextLog log) : IUserContextService, IDisposable
    {
        public Task<IUserContext> GetCurrentContextAsync()
        {
            log.SeenUserIds.Add(scopeUser.UserId);

            IUserContext context = scopeUser.UserId switch
            {
                ActiveAdminId => new UserContext { UserId = ActiveAdminId, RoleId = 1, UserArea = new TestUserArea(CofoundryAdminUserArea.Code) },
                MemberAreaUserId => new UserContext { UserId = MemberAreaUserId, RoleId = 2, UserArea = new TestUserArea("MEM") },
                _ => new UserContext()
            };

            return Task.FromResult(context);
        }

        public Task<IUserContext> GetSystemUserContextAsync() => throw new NotSupportedException();

        public Task<IUserContext> GetCurrentContextByUserAreaAsync(string userAreaCode) => throw new NotSupportedException();

        public void Dispose() => log.DisposedScopes++;
    }

    private sealed class TestUserArea(string code) : IUserAreaDefinition
    {
        public string UserAreaCode => code;

        public string Name => code;

        public bool AllowPasswordSignIn => true;

        public bool UseEmailAsUsername => true;

        public string? SignInPath => null;

        public bool IsDefaultAuthScheme => false;

        public void ConfigureOptions(UserAreaOptions options)
        {
        }
    }
}
