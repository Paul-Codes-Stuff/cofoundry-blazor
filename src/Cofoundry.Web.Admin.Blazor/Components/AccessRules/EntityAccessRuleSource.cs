using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor.Components.AccessRules;

/// <summary>
/// One rule in the editor: a user area, optionally narrowed to a role. <see cref="AccessRuleId"/> is
/// null for a rule added in the editor that hasn't been saved yet.
/// </summary>
public sealed record AccessRuleItem(int? AccessRuleId, string UserAreaCode, string UserAreaName, int? RoleId, string? RoleTitle);

/// <summary>
/// A rule inherited from a parent directory, shown read-only above the entity's own rules.
/// </summary>
public sealed record InheritedAccessRule(
    int PageDirectoryId,
    string DirectoryPath,
    string UserAreaName,
    string? RoleTitle,
    string? UserAreaNameForSignInRedirect,
    AccessRuleViolationAction ViolationAction);

/// <summary>
/// The editable state of an entity's access rule set, the equivalent of the AngularJS
/// EntityAccessEditorController's accessRuleSet plus command.
/// </summary>
public sealed class EntityAccessRuleSetModel
{
    public List<AccessRuleItem> AccessRules { get; set; } = [];

    public IReadOnlyList<InheritedAccessRule> InheritedRules { get; set; } = [];

    public bool RedirectToSignIn { get; set; }

    public string? UserAreaCodeForSignInRedirect { get; set; }

    public AccessRuleViolationAction ViolationAction { get; set; }
}

/// <summary>
/// Loads and saves the access rules of one page or directory, so <see cref="EntityAccessEditorDialog"/>
/// can edit either. This replaces the loader/save functions the AngularJS editor was given as options.
/// </summary>
public abstract class EntityAccessRuleSource
{
    /// <summary>
    /// "Page" or "Directory", used in the dialog title and text.
    /// </summary>
    public abstract string EntityDefinitionName { get; }

    /// <summary>
    /// The entity's full url path, shown in the dialog title.
    /// </summary>
    public abstract string EntityDescription { get; }

    public abstract Task<bool> CanManageAsync(IAdminBlazorExecutor executor);

    /// <summary>
    /// Returns null if the entity no longer exists.
    /// </summary>
    public abstract Task<EntityAccessRuleSetModel?> LoadAsync(IAdminBlazorExecutor executor);

    public abstract Task<AdminCommandResult> SaveAsync(IAdminBlazorExecutor executor, EntityAccessRuleSetModel model);

    protected static Task<bool> HasPermissionAsync<TPermission>(IAdminBlazorExecutor executor)
        where TPermission : IPermissionApplication, new()
    {
        return executor.WithServicesAsync(services =>
            services.GetRequiredService<IPermissionValidationService>().HasPermissionAsync<TPermission>());
    }

    protected static EntityAccessRuleSetModel Map<TRule>(
        IEntityAccessRuleSetDetails<TRule> details,
        IEnumerable<InheritedPageDirectoryAccessDetails> inherited,
        Func<TRule, int> getId)
        where TRule : IEntityAccessRuleSummary
    {
        return new EntityAccessRuleSetModel
        {
            AccessRules = details.AccessRules
                .Select(r => new AccessRuleItem(getId(r), r.UserArea.UserAreaCode, r.UserArea.Name, r.Role?.RoleId, r.Role?.Title))
                .ToList(),
            InheritedRules = inherited
                .SelectMany(set => set.AccessRules.Select(r => new InheritedAccessRule(
                    set.PageDirectory.PageDirectoryId,
                    set.PageDirectory.FullUrlPath,
                    r.UserArea.Name,
                    r.Role?.Title,
                    set.UserAreaForSignInRedirect?.Name,
                    set.ViolationAction)))
                .ToList(),
            RedirectToSignIn = details.UserAreaForSignInRedirect != null,
            UserAreaCodeForSignInRedirect = details.UserAreaForSignInRedirect?.UserAreaCode,
            ViolationAction = details.ViolationAction
        };
    }

    /// <summary>
    /// The sign in redirect only applies when the checkbox is ticked, as in the AngularJS save().
    /// </summary>
    protected static string? GetSignInRedirect(EntityAccessRuleSetModel model) =>
        model.RedirectToSignIn ? model.UserAreaCodeForSignInRedirect : null;
}

public sealed class PageAccessRuleSource(int pageId, string fullUrlPath) : EntityAccessRuleSource
{
    public override string EntityDefinitionName => "Page";

    public override string EntityDescription => fullUrlPath;

    public override Task<bool> CanManageAsync(IAdminBlazorExecutor executor) =>
        HasPermissionAsync<PageAccessRuleManagePermission>(executor);

    public override async Task<EntityAccessRuleSetModel?> LoadAsync(IAdminBlazorExecutor executor)
    {
        var details = await executor.QueryAsync(new GetPageAccessRuleSetDetailsByPageIdQuery(pageId));

        return details == null ? null : Map(details, details.InheritedAccessRules, r => r.PageAccessRuleId);
    }

    public override Task<AdminCommandResult> SaveAsync(IAdminBlazorExecutor executor, EntityAccessRuleSetModel model)
    {
        return executor.ExecuteAsync(new UpdatePageAccessRuleSetCommand
        {
            PageId = pageId,
            AccessRules = model.AccessRules
                .Select(r => new UpdatePageAccessRuleSetCommand.AddOrUpdatePageAccessRuleCommand
                {
                    PageAccessRuleId = r.AccessRuleId,
                    UserAreaCode = r.UserAreaCode,
                    RoleId = r.RoleId
                })
                .ToList(),
            UserAreaCodeForSignInRedirect = GetSignInRedirect(model),
            ViolationAction = model.ViolationAction
        });
    }
}

public sealed class DirectoryAccessRuleSource(int pageDirectoryId, string fullUrlPath) : EntityAccessRuleSource
{
    public override string EntityDefinitionName => "Directory";

    public override string EntityDescription => fullUrlPath;

    public override Task<bool> CanManageAsync(IAdminBlazorExecutor executor) =>
        HasPermissionAsync<PageDirectoryAccessRuleManagePermission>(executor);

    public override async Task<EntityAccessRuleSetModel?> LoadAsync(IAdminBlazorExecutor executor)
    {
        var details = await executor.QueryAsync(new GetPageDirectoryAccessDetailsByPageDirectoryIdQuery(pageDirectoryId));

        return details == null ? null : Map(details, details.InheritedAccessRules, r => r.PageDirectoryAccessRuleId);
    }

    public override Task<AdminCommandResult> SaveAsync(IAdminBlazorExecutor executor, EntityAccessRuleSetModel model)
    {
        return executor.ExecuteAsync(new UpdatePageDirectoryAccessRuleSetCommand
        {
            PageDirectoryId = pageDirectoryId,
            AccessRules = model.AccessRules
                .Select(r => new UpdatePageDirectoryAccessRuleSetCommand.AddOrUpdatePageDirectoryAccessRuleCommand
                {
                    PageDirectoryAccessRuleId = r.AccessRuleId,
                    UserAreaCode = r.UserAreaCode,
                    RoleId = r.RoleId
                })
                .ToList(),
            UserAreaCodeForSignInRedirect = GetSignInRedirect(model),
            ViolationAction = model.ViolationAction
        });
    }
}
