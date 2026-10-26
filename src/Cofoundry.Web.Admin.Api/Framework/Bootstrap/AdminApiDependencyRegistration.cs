using Cofoundry.Core.DependencyInjection;

namespace Cofoundry.Web.Admin.Api.Registration;

public class AdminApiDependencyRegistration : IDependencyRegistration
{
    public void Register(IContainerRegister container)
    {
        container
            .Register<IDashboardContentProvider, DashboardContentProvider>()
            ;
    }
}
