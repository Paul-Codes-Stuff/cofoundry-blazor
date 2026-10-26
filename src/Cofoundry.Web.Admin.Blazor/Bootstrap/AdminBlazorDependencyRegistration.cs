using Cofoundry.Web.Admin.Blazor.Components.DataModels;

namespace Cofoundry.Web.Admin.Blazor;

public class AdminBlazorDependencyRegistration : IDependencyRegistration
{
    public void Register(IContainerRegister container)
    {
        // Overrides Cofoundry's low-priority WebUserSessionService registration. It defers to the
        // web implementation unless a Blazor operation scope has been seeded with a user.
        var sessionServiceOptions = RegistrationOptions.Override();
        sessionServiceOptions.Lifetime = InstanceLifetime.Scoped;

        container
            .Register<AdminBlazorScopeUser>(RegistrationOptions.Scoped())
            .Register<IUserSessionService, CircuitAwareUserSessionService>(sessionServiceOptions)
            .Register<IAdminBlazorExecutor, AdminBlazorExecutor>(RegistrationOptions.Scoped())
            .Register<AdminLookups>(RegistrationOptions.Scoped())
            .Register<BrowserTimeZone>(RegistrationOptions.Scoped())
            .RegisterAll<IAdminBlazorModuleRegistration>(RegistrationOptions.SingletonScope())
            .Register<AdminBlazorModuleRegistry>(RegistrationOptions.SingletonScope())
            .Register<BuiltInAdminModules>()
            .RegisterAll<IDataModelFieldComponentRegistration>(RegistrationOptions.SingletonScope())
            .Register<DataModelFieldComponentRegistry>(RegistrationOptions.SingletonScope())
            .Register<VisualEditorScriptGenerator>()
            .RegisterAll<IVisualEditorRequestExclusionRule>()
            // Above Cofoundry.Web.Admin's low-priority AdminVisualEditorStateService, which does the same.
            .Register<IVisualEditorStateService, AdminBlazorVisualEditorStateService>(RegistrationOptions.Override());
    }
}
