using Cofoundry.Domain;

namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

/// <summary>
/// The slugs are static, like the prefix, so each test sets its own and clears them after.
/// </summary>
public sealed class AdminBlazorRoutesCustomEntityTests : IDisposable
{
    [Fact]
    public void CustomEntities_UsesTheSlugOfThePluralName()
    {
        AdminBlazorRoutes.SetCustomEntitySlugs([new TestDefinition("SIMBLP", "Blog Posts")], []);

        Assert.Equal("/admin-next/blog-posts", AdminBlazorRoutes.CustomEntities("SIMBLP"));
        Assert.Equal("/admin-next/blog-posts", AdminBlazorRoutes.CustomEntities("simblp"));
        Assert.Equal("/admin-next/blog-posts/new", AdminBlazorRoutes.CustomEntityNew("SIMBLP"));
        Assert.Equal("/admin-next/blog-posts/12", AdminBlazorRoutes.CustomEntityDetails("SIMBLP", 12));
        Assert.Equal("SIMBLP", AdminBlazorRoutes.GetCustomEntityDefinitionCode("Blog-Posts"));
    }

    [Fact]
    public void CustomEntities_WithoutASlug_UsesTheCode()
    {
        AdminBlazorRoutes.SetCustomEntitySlugs([new TestDefinition("SIMBLP", "Blog Posts")], []);

        Assert.Equal("/admin-next/custom-entities/SIMAUT", AdminBlazorRoutes.CustomEntities("SIMAUT"));
        Assert.Null(AdminBlazorRoutes.GetCustomEntityDefinitionCode("authors"));
    }

    [Fact]
    public void SlugOfAnotherRoute_KeepsTheCodeUrl()
    {
        AdminBlazorRoutes.SetCustomEntitySlugs(
            [new TestDefinition("SIMPAG", "Pages"), new TestDefinition("SIMCUS", "Custom Entities"), new TestDefinition("SIMAUT", "Authors")],
            ["pages", "users"]);

        Assert.Equal("/admin-next/custom-entities/SIMPAG", AdminBlazorRoutes.CustomEntities("SIMPAG"));
        Assert.Equal("/admin-next/custom-entities/SIMCUS", AdminBlazorRoutes.CustomEntities("SIMCUS"));
        Assert.Equal("/admin-next/authors", AdminBlazorRoutes.CustomEntities("SIMAUT"));
        Assert.Null(AdminBlazorRoutes.GetCustomEntityDefinitionCode("pages"));
        Assert.True(AdminBlazorRoutes.IsReservedSegment("Users"));
        Assert.True(AdminBlazorRoutes.IsReservedSegment("custom-entities"));
        Assert.False(AdminBlazorRoutes.IsReservedSegment("authors"));
    }

    [Fact]
    public void DefinitionsSharingASlug_BothKeepTheirCodeUrls()
    {
        AdminBlazorRoutes.SetCustomEntitySlugs([new TestDefinition("SIMONE", "News Items"), new TestDefinition("SIMTWO", "News items")], []);

        Assert.Equal("/admin-next/custom-entities/SIMONE", AdminBlazorRoutes.CustomEntities("SIMONE"));
        Assert.Equal("/admin-next/custom-entities/SIMTWO", AdminBlazorRoutes.CustomEntities("SIMTWO"));
        Assert.Null(AdminBlazorRoutes.GetCustomEntityDefinitionCode("news-items"));
    }

    public void Dispose()
    {
        AdminBlazorRoutes.SetCustomEntitySlugs([], []);
    }

    private sealed class TestDefinition(string code, string namePlural) : ICustomEntityDefinition
    {
        public string CustomEntityDefinitionCode => code;

        public string Name => namePlural;

        public string NamePlural => namePlural;

        public string Description => namePlural;

        public bool ForceUrlSlugUniqueness => false;

        public bool HasLocale => false;

        public bool AutoGenerateUrlSlug => true;

        public bool AutoPublish => false;
    }
}
