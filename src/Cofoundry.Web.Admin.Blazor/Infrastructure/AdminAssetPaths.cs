namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Paths to the static files served from this library's wwwroot.
/// </summary>
public static class AdminAssetPaths
{
    public const string ContentRoot = "/_content/Cofoundry.Web.Admin.Blazor";

    public const string ImageReplacement = ContentRoot + "/img/AssetReplacement/image-replacement.png";

    public const string PreviewNotSupported = ContentRoot + "/img/AssetReplacement/preview-not-supported.png";

    public const string ImageUploadScript = ContentRoot + "/js/image-upload.js";

    public const string TimeZoneScript = ContentRoot + "/js/time-zone.js";

    public const string OptionSourceScript = ContentRoot + "/js/option-source.js";

    public const string SessionCheckScript = ContentRoot + "/js/session-check.js";

    public const string HtmlEditorScript = ContentRoot + "/js/html-editor.js";

    /// <summary>
    /// TinyMCE 4.4.3, the build the AngularJS admin bundles (its skin is in admin.css).
    /// </summary>
    public const string TinyMceScript = ContentRoot + "/lib/tinymce/tinymce.min.js";

    public const string TinyMceContentCss = ContentRoot + "/css/third-party/tinymce/content.min.css";
}
