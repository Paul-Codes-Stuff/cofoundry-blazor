using Cofoundry.Core.DependencyInjection;
using Cofoundry.Plugins.YouTube.Admin.Blazor.Components;
using Cofoundry.Web.Admin.Blazor.Components.DataModels;

namespace Cofoundry.Plugins.YouTube.Admin.Blazor;

/// <summary>
/// Edits <see cref="YouTubeAttribute"/> properties in the Blazor admin's data model forms, as the
/// AngularJS package's cms-form-field-youtube does in the AngularJS admin.
/// </summary>
public class YouTubeDataModelFieldComponentRegistration : IDataModelFieldComponentRegistration
{
    public IEnumerable<KeyValuePair<string, Type>> GetComponents()
    {
        // The template hint YouTubeAttribute sets.
        yield return new("Youtube", typeof(DataModelYouTubeField));
    }
}

public class YouTubeBlazorDependencyRegistration : IDependencyRegistration
{
    public void Register(IContainerRegister container)
    {
        container.Register<YouTubeVideoInfoService>(RegistrationOptions.SingletonScope());
    }
}
