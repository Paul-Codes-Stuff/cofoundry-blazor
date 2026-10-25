using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// The user area settings the users screens need, as the AngularJS admin's UsersModuleOptions.
/// </summary>
public sealed record UserAreaInfo(
    string UserAreaCode,
    string Name,
    bool AllowPasswordSignIn,
    bool UseEmailAsUsername,
    bool ShowDisplayName)
{
    public bool IsCofoundryAdminArea => UserAreaCode == CofoundryAdminUserArea.Code;

    /// <summary>
    /// Returns null if no user area has the code.
    /// </summary>
    public static Task<UserAreaInfo?> LoadAsync(IAdminBlazorExecutor executor, string userAreaCode)
    {
        return executor.WithServicesAsync(services =>
        {
            var repository = services.GetRequiredService<IUserAreaDefinitionRepository>();
            var definition = repository.GetByCode(userAreaCode);

            return Task.FromResult(definition == null ? null : Map(repository, definition));
        });
    }

    public static Task<IReadOnlyList<UserAreaInfo>> LoadAllAsync(IAdminBlazorExecutor executor)
    {
        return executor.WithServicesAsync(services =>
        {
            var repository = services.GetRequiredService<IUserAreaDefinitionRepository>();
            IReadOnlyList<UserAreaInfo> areas = repository.GetAll().Select(d => Map(repository, d)).ToList();

            return Task.FromResult(areas);
        });
    }

    private static UserAreaInfo Map(IUserAreaDefinitionRepository repository, IUserAreaDefinition definition)
    {
        var options = repository.GetOptionsByCode(definition.UserAreaCode);

        return new UserAreaInfo(
            definition.UserAreaCode,
            definition.Name,
            definition.AllowPasswordSignIn,
            definition.UseEmailAsUsername,
            !options.Username.UseAsDisplayName);
    }
}

/// <summary>
/// The signed-in admin user, for the checks the AngularJS admin made against its currentUser
/// (e.g. you can't delete yourself or change your own role).
/// </summary>
public sealed record CurrentAdminUser(int? UserId, int? RoleId, bool IsSuperAdmin)
{
    public static async Task<CurrentAdminUser> LoadAsync(IAdminBlazorExecutor executor)
    {
        var context = await executor.QueryAsync(new GetCurrentUserContextQuery());

        return new CurrentAdminUser(context.UserId, context.RoleId, context.RoleCode == SuperAdminRole.Code);
    }
}
