using Cofoundry.Core.Configuration;

namespace Cofoundry.Web.Admin.Api;

/// <summary>
/// Settings for the admin API, in the "Cofoundry:AdminApi" configuration section.
/// </summary>
public class AdminApiSettings : CofoundryConfigurationSettingsBase
{
    /// <summary>
    /// The path the API is served under, without leading or trailing slashes, e.g. "admin/api". Defaults to
    /// "{Cofoundry:Admin:DirectoryName}/api", where Cofoundry.Web.Admin serves its own copy of the API. While
    /// Cofoundry.Web.Admin is installed, set a different path, e.g. "admin-next/api".
    /// </summary>
    [RegularExpression("^[a-zA-Z0-9-]+(/[a-zA-Z0-9-]+)*$", ErrorMessage = "The base url can only contain letters, numbers, dashes and slashes between segments e.g. \"admin/api\".")]
    public string? BaseUrl
    {
        get { return _baseUrl; }
        set { _baseUrl = value?.Trim('/').ToLowerInvariant(); }
    }
    private string? _baseUrl;

    /// <summary>
    /// Removes the API's routes. The API is also removed when the admin panel is disabled
    /// (<see cref="AdminSettings.Disabled"/>).
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// The path the API is served under: <see cref="BaseUrl"/>, or by default the admin directory followed
    /// by "/api".
    /// </summary>
    public string GetBaseUrl(AdminSettings adminSettings)
    {
        ArgumentNullException.ThrowIfNull(adminSettings);

        return string.IsNullOrEmpty(BaseUrl) ? adminSettings.DirectoryName + "/api" : BaseUrl;
    }
}
