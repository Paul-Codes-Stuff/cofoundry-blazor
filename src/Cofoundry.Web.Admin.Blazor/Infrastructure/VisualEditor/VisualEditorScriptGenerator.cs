using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cofoundry.Web.Admin.Blazor.Components.VisualEditor;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Creates the markup <see cref="VisualEditorActionResult"/> injects into a page: the stylesheet and script
/// for the head, and the page data, toolbar and frame for the body.
/// </summary>
public class VisualEditorScriptGenerator
{
    // Html-safe by default (the encoder escapes <, > and &), so it can sit in a script element.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IPageResponseDataCache _pageResponseDataCache;
    private readonly IPermissionValidationService _permissionValidationService;
    private readonly ILoggerFactory _loggerFactory;
    private readonly AdminBlazorSettings _adminBlazorSettings;

    public VisualEditorScriptGenerator(
        IPageResponseDataCache pageResponseDataCache,
        IPermissionValidationService permissionValidationService,
        ILoggerFactory loggerFactory,
        AdminBlazorSettings adminBlazorSettings
        )
    {
        _pageResponseDataCache = pageResponseDataCache;
        _permissionValidationService = permissionValidationService;
        _loggerFactory = loggerFactory;
        _adminBlazorSettings = adminBlazorSettings;
    }

    public string CreateHeadScript()
    {
        var head = $"""
            	<link rel="stylesheet" href="{AdminAssetPaths.VisualEditorStyles}" />
            	<script src="{AdminAssetPaths.VisualEditorScript}" defer></script>
            """;

        if (!_adminBlazorSettings.VisualEditorFontAwesome)
        {
            return head;
        }

        // First, so the editor's own styles win where the two overlap.
        var url = string.IsNullOrWhiteSpace(_adminBlazorSettings.VisualEditorFontAwesomeUrl)
            ? AdminAssetPaths.FontAwesomeStyles
            : _adminBlazorSettings.VisualEditorFontAwesomeUrl.Trim();

        return $"\t<link rel=\"stylesheet\" href=\"{HtmlEncoder.Default.Encode(url)}\" />" + Environment.NewLine + head;
    }

    /// <summary>
    /// Pages that aren't Cofoundry pages (e.g. a plain MVC view) have no page data: they get the toolbar
    /// only, labelled "Static page".
    /// </summary>
    public async Task<string> CreateBodyScriptAsync(HttpContext httpContext)
    {
        var responseData = _pageResponseDataCache.Get();
        var pageData = responseData == null ? null : VisualEditorPageData.Create(responseData, _permissionValidationService);
        var toolbar = await RenderToolbarAsync(httpContext, responseData, pageData);

        if (pageData == null)
        {
            return toolbar;
        }

        var json = JsonSerializer.Serialize(pageData, JsonOptions);

        return $"""<script type="application/json" id="cofoundry-sv__data">{json}</script>""" + Environment.NewLine + toolbar;
    }

    private async Task<string> RenderToolbarAsync(HttpContext httpContext, IPageResponseData? responseData, VisualEditorPageData? pageData)
    {
        await using var renderer = new HtmlRenderer(httpContext.RequestServices, _loggerFactory);

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(VisualEditorToolbar.ResponseData)] = responseData,
                [nameof(VisualEditorToolbar.PageData)] = pageData,
                [nameof(VisualEditorToolbar.RequestPath)] = httpContext.Request.Path.Value ?? "/"
            });
            var output = await renderer.RenderComponentAsync<VisualEditorToolbar>(parameters);

            return output.ToHtmlString();
        });
    }
}
