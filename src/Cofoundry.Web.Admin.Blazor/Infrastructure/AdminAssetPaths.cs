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

    /// <summary>
    /// Opens and closes the nav on small screens, on every page (static SSR ones too).
    /// </summary>
    public const string MobileNavScript = ContentRoot + "/js/mobile-nav.js";

    /// <summary>
    /// Injected into the site's pages for admins, with <see cref="VisualEditorStyles"/>. Plain JavaScript with no
    /// dependencies, since it runs alongside the site's own scripts.
    /// </summary>
    public const string VisualEditorScript = ContentRoot + "/js/visual-editor.js";

    public const string VisualEditorStyles = ContentRoot + "/css/visual-editor.css";

    /// <summary>
    /// Font Awesome 4.3 on its own, for the visual editor's icons (<see cref="AdminBlazorSettings.VisualEditorFontAwesome"/>).
    /// </summary>
    public const string FontAwesomeStyles = ContentRoot + "/css/font-awesome.css";

    public const string VisualEditorLogo = ContentRoot + "/img/visual-editor/logo-thumb.png";

    /// <summary>
    /// Loaded by the visual editor's frame page (<c>Components/VisualEditor/VisualEditorFrame.razor</c>).
    /// </summary>
    public const string VisualEditorFrameScript = ContentRoot + "/js/visual-editor-frame.js";

    public const string HtmlEditorScript = ContentRoot + "/js/html-editor.js";

    /// <summary>
    /// TinyMCE 4.4.3, the build the AngularJS admin bundles (its skin is in admin.css).
    /// </summary>
    public const string TinyMceScript = ContentRoot + "/lib/tinymce/tinymce.min.js";

    public const string TinyMceContentCss = ContentRoot + "/css/third-party/tinymce/content.min.css";
}
