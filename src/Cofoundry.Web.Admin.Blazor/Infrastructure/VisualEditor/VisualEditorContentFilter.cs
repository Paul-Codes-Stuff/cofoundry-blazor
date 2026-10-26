using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// A global MVC result filter that adds the visual editor (toolbar, region and block overlays, and the
/// frame its dialogs open in) to the site's html pages for signed-in admin users. A port of
/// Cofoundry.Web.Admin's VisualEditorContentFilter; see <see cref="VisualEditorInstallation"/> for when
/// it's used.
/// </summary>
public partial class VisualEditorContentFilter : IAsyncResultFilter
{
    private readonly IUserContextService _userContextService;
    private readonly IEnumerable<IVisualEditorRequestExclusionRule> _exclusionRules;
    private readonly VisualEditorScriptGenerator _scriptGenerator;
    private readonly AdminSettings _adminSettings;

    public VisualEditorContentFilter(
        IUserContextService userContextService,
        IEnumerable<IVisualEditorRequestExclusionRule> exclusionRules,
        VisualEditorScriptGenerator scriptGenerator,
        AdminSettings adminSettings
        )
    {
        _userContextService = userContextService;
        _exclusionRules = exclusionRules;
        _scriptGenerator = scriptGenerator;
        _adminSettings = adminSettings;
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (CanShowVisualEditor(context) && await IsAdminUserAsync())
        {
            context.Result = new VisualEditorActionResult(context.Result, _scriptGenerator);
        }

        await next();
    }

    private bool CanShowVisualEditor(ResultExecutingContext context)
    {
        var request = context.HttpContext.Request;

        return !_adminSettings.Disabled
            && IsValidActionType(context.Result)
            && request.Headers.XRequestedWith != "XMLHttpRequest"
            // A page rather than a file, which would have an extension.
            && PagePathRegex().IsMatch(request.Path)
            && !_exclusionRules.Any(r => r.ShouldExclude(request));
    }

    private static bool IsValidActionType(IActionResult actionResult)
    {
        return actionResult switch
        {
            FileResult => false,
            StatusCodeResult => false,
            // The AngularJS editor excluded Razor Pages too: they don't render through the MVC pipeline the
            // editor wraps.
            Microsoft.AspNetCore.Mvc.RazorPages.PageResult => false,
            _ => true
        };
    }

    private async Task<bool> IsAdminUserAsync()
    {
        // The ambient scheme may be another user area's, so ask for the admin's specifically.
        var userContext = await _userContextService.GetCurrentContextByUserAreaAsync(CofoundryAdminUserArea.Code);

        return userContext.IsCofoundryUser();
    }

    [GeneratedRegex(@"^(?!(.*[\.].*))", RegexOptions.IgnoreCase)]
    private static partial Regex PagePathRegex();
}

/// <summary>
/// Runs the wrapped result into a buffer, then injects the visual editor into it if it's an html document.
/// Anything else is written back unchanged.
/// </summary>
public class VisualEditorActionResult : IActionResult
{
    // No byte order mark: Cofoundry.Web.Admin's version wrote one in front of every response it wrapped.
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly IActionResult _wrappedActionResult;
    private readonly VisualEditorScriptGenerator _scriptGenerator;

    public VisualEditorActionResult(IActionResult wrappedActionResult, VisualEditorScriptGenerator scriptGenerator)
    {
        _wrappedActionResult = wrappedActionResult;
        _scriptGenerator = scriptGenerator;
    }

    public async Task ExecuteResultAsync(ActionContext context)
    {
        var response = context.HttpContext.Response;
        var originalBody = response.Body;
        using var buffer = new MemoryStream();
        response.Body = buffer;

        try
        {
            await _wrappedActionResult.ExecuteResultAsync(context);
        }
        finally
        {
            response.Body = originalBody;
        }

        buffer.Seek(0, SeekOrigin.Begin);

        if (!IsHtmlContentType(response.ContentType))
        {
            await buffer.CopyToAsync(originalBody);
            return;
        }

        var html = Utf8.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);

        if (html.TrimStart().StartsWith("<?xml", StringComparison.Ordinal))
        {
            await buffer.CopyToAsync(originalBody);
            return;
        }

        var headScript = _scriptGenerator.CreateHeadScript();
        var bodyScript = await _scriptGenerator.CreateBodyScriptAsync(context.HttpContext);
        html = HtmlDocumentScriptInjector.InjectScripts(html, headScript, bodyScript);

        var bytes = Utf8.GetBytes(html);
        if (!response.HasStarted)
        {
            response.ContentLength = null;
        }

        await originalBody.WriteAsync(bytes);
    }

    private static bool IsHtmlContentType(string? contentType)
    {
        return StringHelper
            .SplitAndTrim(contentType, ';')
            .Contains("text/html");
    }
}

/// <summary>
/// Inserts markup before the end of an html document's head and body.
/// </summary>
public static class HtmlDocumentScriptInjector
{
    private const string HeadTagStart = "<head>";
    private const string HeadTagEnd = "</head>";
    private const string BodyTagStart = "<body";
    private const string BodyTagEnd = "</body>";

    /// <summary>
    /// Adds <paramref name="headScript"/> before &lt;/head&gt; (adding a head if there's none) and
    /// <paramref name="bodyScript"/> before the last &lt;/body&gt;. A document without a body is
    /// returned with only the head script.
    /// </summary>
    public static string InjectScripts(string html, string? headScript, string? bodyScript)
    {
        html = InjectHeadScript(html, headScript);

        if (string.IsNullOrWhiteSpace(bodyScript))
        {
            return html;
        }

        var insertBodyIndex = html.LastIndexOf(BodyTagEnd, StringComparison.OrdinalIgnoreCase);
        if (insertBodyIndex < 0)
        {
            return html;
        }

        return string.Concat(html.AsSpan(0, insertBodyIndex), bodyScript, Environment.NewLine, html.AsSpan(insertBodyIndex));
    }

    private static string InjectHeadScript(string html, string? headScript)
    {
        if (string.IsNullOrWhiteSpace(headScript))
        {
            return html;
        }

        var insertHeadIndex = html.IndexOf(HeadTagEnd, StringComparison.OrdinalIgnoreCase);
        if (insertHeadIndex > 0)
        {
            return string.Concat(html.AsSpan(0, insertHeadIndex), headScript, Environment.NewLine, html.AsSpan(insertHeadIndex));
        }

        var bodyStartIndex = html.IndexOf(BodyTagStart, StringComparison.OrdinalIgnoreCase);
        if (bodyStartIndex > 0)
        {
            return string.Concat(
                html.AsSpan(0, bodyStartIndex),
                HeadTagStart + Environment.NewLine + headScript + Environment.NewLine + HeadTagEnd + Environment.NewLine,
                html.AsSpan(bodyStartIndex));
        }

        return html;
    }
}
