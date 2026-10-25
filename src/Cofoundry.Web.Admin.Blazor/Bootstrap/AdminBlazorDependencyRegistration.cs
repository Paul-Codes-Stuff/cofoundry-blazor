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
            .RegisterAll<IDataModelFieldComponentRegistration>(RegistrationOptions.SingletonScope())
            .Register<DataModelFieldComponentRegistry>(RegistrationOptions.SingletonScope());
    }
}
