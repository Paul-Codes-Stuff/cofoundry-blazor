namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure
{
    // CreateHeadScript only reads the settings, so the other dependencies are left out.
    public class VisualEditorScriptGeneratorTests
    {
        private static string CreateHeadScript(AdminBlazorSettings settings)
        {
            return new VisualEditorScriptGenerator(null!, null!, null!, settings).CreateHeadScript();
        }

        [Fact]
        public void CreateHeadScript_FontAwesomeOffByDefault()
        {
            var head = CreateHeadScript(new AdminBlazorSettings());

            Assert.Contains(AdminAssetPaths.VisualEditorStyles, head);
            Assert.DoesNotContain("font-awesome", head);
        }

        [Fact]
        public void CreateHeadScript_FontAwesome_UsesTheAdminsCopy()
        {
            var head = CreateHeadScript(new AdminBlazorSettings { VisualEditorFontAwesome = true });

            Assert.Contains($"<link rel=\"stylesheet\" href=\"{AdminAssetPaths.FontAwesomeStyles}\" />", head);
            Assert.True(head.IndexOf(AdminAssetPaths.FontAwesomeStyles) < head.IndexOf(AdminAssetPaths.VisualEditorStyles));
        }

        [Fact]
        public void CreateHeadScript_FontAwesomeUrl_IsUsedAndEncoded()
        {
            var head = CreateHeadScript(new AdminBlazorSettings
            {
                VisualEditorFontAwesome = true,
                VisualEditorFontAwesomeUrl = " https://cdn.example.com/fa.css?a=1&b=\"2\" "
            });

            Assert.Contains("href=\"https://cdn.example.com/fa.css?a=1&amp;b=&quot;2&quot;\"", head);
            Assert.DoesNotContain(AdminAssetPaths.FontAwesomeStyles, head);
        }

        [Fact]
        public void CreateHeadScript_FontAwesomeUrl_IgnoredWhenOff()
        {
            var head = CreateHeadScript(new AdminBlazorSettings { VisualEditorFontAwesomeUrl = "https://cdn.example.com/fa.css" });

            Assert.DoesNotContain("cdn.example.com", head);
        }
    }
}
