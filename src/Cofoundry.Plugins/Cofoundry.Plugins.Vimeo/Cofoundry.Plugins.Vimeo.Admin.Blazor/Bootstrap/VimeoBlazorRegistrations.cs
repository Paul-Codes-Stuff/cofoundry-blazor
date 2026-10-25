using Cofoundry.Core.DependencyInjection;
using Cofoundry.Plugins.Vimeo.Admin.Blazor.Components;
using Cofoundry.Web.Admin.Blazor.Components.DataModels;

namespace Cofoundry.Plugins.Vimeo.Admin.Blazor;

/// <summary>
/// Edits <see cref="VimeoAttribute"/> properties in the Blazor admin's data model forms, as the
/// AngularJS package's cms-form-field-vimeo does in the AngularJS admin.
/// </summary>
public class VimeoDataModelFieldComponentRegistration : IDataModelFieldComponentRegistration
{
    public IEnumerable<KeyValuePair<string, Type>> GetComponents()
    {
        // The template hint VimeoAttribute sets.
        yield return new("Vimeo", typeof(DataModelVimeoField));
    }
}

public class VimeoBlazorDependencyRegistration : IDependencyRegistration
{
    public void Register(IContainerRegister container)
    {
        container.Register<VimeoVideoInfoService>(RegistrationOptions.SingletonScope());
    }
}
