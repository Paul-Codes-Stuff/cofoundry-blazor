using Cofoundry.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure
{
    // Cofoundry.Web.Admin isn't loaded in these tests, so the Blazor editor is used whatever UseVisualEditor says.
    public class VisualEditorInstallationTests
    {
        [Fact]
        public void IsEnabled_ByDefault()
        {
            Assert.False(VisualEditorInstallation.IsAngularAdminInstalled());
            Assert.True(VisualEditorInstallation.IsEnabled(new AdminSettings(), new PagesSettings(), new AdminBlazorSettings()));
        }

        [Fact]
        public void IsEnabled_OffWhenTheAdminOrPagesAreDisabled()
        {
            Assert.False(VisualEditorInstallation.IsEnabled(new AdminSettings { Disabled = true }, new PagesSettings(), new AdminBlazorSettings()));
            Assert.False(VisualEditorInstallation.IsEnabled(new AdminSettings(), new PagesSettings { Disabled = true }, new AdminBlazorSettings()));
            Assert.False(VisualEditorInstallation.IsEnabled(new AdminSettings { AutoInjectVisualEditor = false }, new PagesSettings(), new AdminBlazorSettings()));
        }

        [Fact]
        public void ConfigureFilters_ReplacesTheAngularFilter()
        {
            var options = new MvcOptions();
            options.Filters.Add(typeof(Cofoundry.Web.Admin.Internal.VisualEditorContentFilter));
            options.Filters.Add(typeof(OtherFilter));

            VisualEditorInstallation.ConfigureFilters(options, new AdminSettings(), new PagesSettings(), new AdminBlazorSettings());

            var types = options.Filters.OfType<TypeFilterAttribute>().Select(f => f.ImplementationType).ToList();
            Assert.Equal([typeof(OtherFilter), typeof(VisualEditorContentFilter)], types);
        }

        [Fact]
        public void ConfigureFilters_Disabled_LeavesTheFilters()
        {
            var options = new MvcOptions();
            options.Filters.Add(typeof(Cofoundry.Web.Admin.Internal.VisualEditorContentFilter));

            VisualEditorInstallation.ConfigureFilters(options, new AdminSettings { AutoInjectVisualEditor = false }, new PagesSettings(), new AdminBlazorSettings());

            var filter = Assert.IsType<TypeFilterAttribute>(Assert.Single(options.Filters));
            Assert.Equal(typeof(Cofoundry.Web.Admin.Internal.VisualEditorContentFilter), filter.ImplementationType);
        }

        private sealed class OtherFilter : Microsoft.AspNetCore.Mvc.Filters.IResultFilter
        {
            public void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }

            public void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
        }
    }
}

// Stands in for the AngularJS admin's filter, which is matched by its full name.
namespace Cofoundry.Web.Admin.Internal
{
    public sealed class VisualEditorContentFilter : Microsoft.AspNetCore.Mvc.Filters.IResultFilter
    {
        public void OnResultExecuting(Microsoft.AspNetCore.Mvc.Filters.ResultExecutingContext context) { }

        public void OnResultExecuted(Microsoft.AspNetCore.Mvc.Filters.ResultExecutedContext context) { }
    }
}
