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

    /// <summary>
    /// Uses the Blazor admin's visual editor on the site's pages instead of the AngularJS one, while
    /// Cofoundry.Web.Admin is installed. Off by default. Without Cofoundry.Web.Admin the Blazor editor is
    /// always used (see <see cref="VisualEditorInstallation"/>).
    /// </summary>
    public bool UseVisualEditor { get; set; }

    /// <summary>
    /// Adds Font Awesome to the site's pages along with the visual editor, whose toolbar and block menu use its
    /// icons. Off by default: turn it on for sites that don't load Font Awesome themselves. The icons are
    /// Font Awesome 4 classes, and its <c>.fa</c> rules can clash with a site's own, newer Font Awesome.
    /// </summary>
    public bool VisualEditorFontAwesome { get; set; }

    /// <summary>
    /// Where <see cref="VisualEditorFontAwesome"/> loads Font Awesome's stylesheet from. Defaults to the admin's
    /// own copy of Font Awesome 4.3; set it to use a CDN instead, e.g.
    /// "https://cdnjs.cloudflare.com/ajax/libs/font-awesome/4.3.0/css/font-awesome.min.css".
    /// </summary>
    public string? VisualEditorFontAwesomeUrl { get; set; }
}
