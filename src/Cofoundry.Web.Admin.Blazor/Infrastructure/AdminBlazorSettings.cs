using Cofoundry.Core.Configuration;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Settings for the Blazor admin, in the "Cofoundry:AdminBlazor" configuration section.
/// </summary>
public class AdminBlazorSettings : CofoundryConfigurationSettingsBase
{
    /// <summary>
    /// The path to the Blazor admin, like the AngularJS admin's <see cref="AdminSettings.DirectoryName"/>.
    /// Defaults to "admin-next". Can only contain letters, numbers and dashes. While both admins are
    /// installed, it must differ from <see cref="AdminSettings.DirectoryName"/>.
    /// </summary>
    [Required]
    [Slug]
    public string DirectoryName
    {
        get { return _directoryName; }
        set { _directoryName = value.ToLowerInvariant(); }
    }
    private string _directoryName = AdminBlazorRoutes.DefaultPrefix;

    /// <summary>
    /// Points the links in the admin user area's emails (reset password, sign in) at the Blazor admin
    /// instead of the AngularJS admin. Off by default, so installing the Blazor admin alongside the
    /// AngularJS one doesn't change what existing admin users are sent. It applies to emails sent from
    /// either admin.
    /// </summary>
    public bool UseForEmailLinks { get; set; }
}
