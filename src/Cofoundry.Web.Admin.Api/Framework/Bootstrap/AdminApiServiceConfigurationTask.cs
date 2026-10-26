using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Cofoundry.Web.Admin.Api;

public class AdminApiServiceConfigurationTask : IStartupServiceConfigurationTask
{
    public void ConfigureServices(IMvcBuilder mvcBuilder)
    {
        mvcBuilder.Services.TryAddEnumerable(ServiceDescriptor.Transient<IActionDescriptorProvider, AdminApiActionDescriptorProvider>());
    }
}
