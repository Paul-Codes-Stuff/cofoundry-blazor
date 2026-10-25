namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

public class AdminBlazorRoutesTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/admin-next/pages")]
    [InlineData("/admin-next/pages?pageDirectoryId=1")]
    public void IsLocalUrl_WhenSitePath_ReturnsTrue(string url)
    {
        Assert.True(AdminBlazorRoutes.IsLocalUrl(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("admin-next/pages")]
    [InlineData("https://example.com/")]
    [InlineData("//example.com/")]
    [InlineData("/\\example.com/")]
    public void IsLocalUrl_WhenNotSitePath_ReturnsFalse(string? url)
    {
        Assert.False(AdminBlazorRoutes.IsLocalUrl(url));
    }

    [Fact]
    public void LoginWithReturnUrl_EscapesReturnUrl()
    {
        var result = AdminBlazorRoutes.LoginWithReturnUrl("/admin-next/pages?a=1&b=2");

        Assert.Equal("/admin-next/auth/login?ReturnUrl=%2Fadmin-next%2Fpages%3Fa%3D1%26b%3D2", result);
    }

    [Theory]
    [InlineData(null, "/admin-next/auth/change-password")]
    [InlineData("", "/admin-next/auth/change-password")]
    [InlineData("/admin-next/users", "/admin-next/auth/change-password?ReturnUrl=%2Fadmin-next%2Fusers")]
    public void ChangePassword_AddsReturnUrlWhenSet(string? returnUrl, string expected)
    {
        Assert.Equal(expected, AdminBlazorRoutes.ChangePassword(returnUrl));
    }
}
