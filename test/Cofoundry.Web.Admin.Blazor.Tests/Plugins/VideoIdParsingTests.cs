using Cofoundry.Plugins.Vimeo.Admin.Blazor;
using Cofoundry.Plugins.YouTube.Admin.Blazor;

namespace Cofoundry.Web.Admin.Blazor.Tests.Plugins;

public class VideoIdParsingTests
{
    [Theory]
    [InlineData("dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData(" dQw4w9WgXcQ ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/watch?feature=share&v=dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ", "dQw4w9WgXcQ")]
    [InlineData("https://example.com/watch?v=x", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void YouTube_ParseVideoId(string? urlOrId, string? expected)
    {
        Assert.Equal(expected, YouTubeVideoInfoService.ParseVideoId(urlOrId));
    }

    [Theory]
    [InlineData("22439234", 22439234L)]
    [InlineData("https://vimeo.com/22439234", 22439234L)]
    [InlineData("https://vimeo.com/channels/staffpicks/22439234", 22439234L)]
    [InlineData("https://vimeo.com/groups/shortfilms/videos/22439234", 22439234L)]
    [InlineData("https://example.com/22439234", null)]
    [InlineData("abc", null)]
    [InlineData(null, null)]
    public void Vimeo_ParseVideoId(string? urlOrId, long? expected)
    {
        Assert.Equal(expected, VimeoVideoInfoService.ParseVideoId(urlOrId));
    }
}
