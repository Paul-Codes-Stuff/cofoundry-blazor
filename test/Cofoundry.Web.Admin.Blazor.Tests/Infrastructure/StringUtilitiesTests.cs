namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

public class StringUtilitiesTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  Café & Bar!  ", "cafe-and-bar")]
    [InlineData("one_two.three", "one-two-three")]
    [InlineData("a -- b", "a-b")]
    [InlineData("-leading", "leading")]
    public void Slugify_MakesUrlPath(string value, string expected)
    {
        Assert.Equal(expected, StringUtilities.Slugify(value));
    }

    [Fact]
    public void Slugify_TruncatesResultWithoutTrailingHyphen()
    {
        Assert.Equal("hello", StringUtilities.Slugify("Hello World", 6));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Slugify_WhenEmpty_ReturnsValue(string? value)
    {
        Assert.Equal(value, StringUtilities.Slugify(value));
    }

    [Theory]
    [InlineData(0, "0 kb")]
    [InlineData(500, "500.0 bytes")]
    [InlineData(1536, "1.5 kb")]
    [InlineData(1048576, "1.0 mb")]
    public void FormatBytes_UsesLargestUnit(long bytes, string expected)
    {
        Assert.Equal(expected, StringUtilities.FormatBytes(bytes));
    }

    [Theory]
    [InlineData("my-photo.jpg", "My-photo")]
    [InlineData("report.final.pdf", "Report.final")]
    public void TitleFromFileName_DropsExtensionAndCapitalises(string fileName, string expected)
    {
        Assert.Equal(expected, StringUtilities.TitleFromFileName(fileName));
    }

    [Theory]
    [InlineData("TopLeft", "Top Left")]
    [InlineData("Centre", "Centre")]
    public void SplitPascalCase_AddsSpaces(string value, string expected)
    {
        Assert.Equal(expected, StringUtilities.SplitPascalCase(value));
    }
}
