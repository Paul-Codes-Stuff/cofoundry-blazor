namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

public class HtmlDocumentScriptInjectorTests
{
    private static readonly string NewLine = Environment.NewLine;

    [Fact]
    public void InjectScripts_AddsBeforeHeadAndBodyEnd()
    {
        var html = "<html><head><title>T</title></head><body><p>x</p></body></html>";

        var result = HtmlDocumentScriptInjector.InjectScripts(html, "<link h>", "<script b>");

        Assert.Equal($"<html><head><title>T</title><link h>{NewLine}</head><body><p>x</p><script b>{NewLine}</body></html>", result);
    }

    [Fact]
    public void InjectScripts_WithoutHead_AddsOneBeforeBody()
    {
        var html = "<html><body class=\"a\"></body></html>";

        var result = HtmlDocumentScriptInjector.InjectScripts(html, "<link h>", null);

        Assert.Equal($"<html><head>{NewLine}<link h>{NewLine}</head>{NewLine}<body class=\"a\"></body></html>", result);
    }

    [Fact]
    public void InjectScripts_UsesTheLastBodyEnd()
    {
        // e.g. a page that shows html in a code sample.
        var html = "<html><head></head><body><pre>&lt;/body&gt;</pre><!-- </body> --></body></html>";

        var result = HtmlDocumentScriptInjector.InjectScripts(html, null, "<script b>");

        Assert.EndsWith($"<!-- </body> --><script b>{NewLine}</body></html>", result);
    }

    [Fact]
    public void InjectScripts_IgnoresCase()
    {
        var html = "<HTML><HEAD></HEAD><BODY></BODY></HTML>";

        var result = HtmlDocumentScriptInjector.InjectScripts(html, "<link h>", "<script b>");

        Assert.Equal($"<HTML><HEAD><link h>{NewLine}</HEAD><BODY><script b>{NewLine}</BODY></HTML>", result);
    }

    [Theory]
    [InlineData("just text")]
    [InlineData("<div>a fragment</div>")]
    public void InjectScripts_NotADocument_Unchanged(string html)
    {
        var result = HtmlDocumentScriptInjector.InjectScripts(html, "<link h>", "<script b>");

        Assert.Equal(html, result);
    }
}
