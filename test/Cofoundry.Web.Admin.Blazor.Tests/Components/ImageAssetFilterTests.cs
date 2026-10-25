using Cofoundry.Web.Admin.Blazor.Components.Images;

namespace Cofoundry.Web.Admin.Blazor.Tests.Components;

public class ImageAssetFilterTests
{
    [Theory]
    [InlineData(460, 460, true)]
    [InlineData(800, 600, true)]
    [InlineData(459, 600, false)]
    [InlineData(600, 459, false)]
    public void IsSizeMet_ChecksMinSize(int width, int height, bool expected)
    {
        var filter = new ImageAssetFilter { MinWidth = 460, MinHeight = 460 };

        Assert.Equal(expected, filter.IsSizeMet(width, height));
    }

    [Theory]
    [InlineData(300, 200, true)]
    [InlineData(301, 200, false)]
    [InlineData(300, 199, false)]
    public void IsSizeMet_ChecksExactSize(int width, int height, bool expected)
    {
        var filter = new ImageAssetFilter { Width = 300, Height = 200 };

        Assert.Equal(expected, filter.IsSizeMet(width, height));
    }

    [Fact]
    public void IsSizeMet_IgnoresRestrictionsOfZeroOrOne()
    {
        var filter = new ImageAssetFilter { Width = 1, MinHeight = 0 };

        Assert.True(filter.IsSizeMet(10, 10));
        Assert.True(ImageAssetFilter.None.IsSizeMet(10, 10));
    }

    [Fact]
    public void DescribeSize_ListsRestrictions()
    {
        var filter = new ImageAssetFilter { Width = 300, MinWidth = 100, MinHeight = 50 };

        Assert.Equal("width 300, min-size 100x50", filter.DescribeSize());
    }
}
