using Cofoundry.Web.Admin.Blazor.Components.Ui;

namespace Cofoundry.Web.Admin.Blazor.Tests.Components;

public class FieldValidationMessageTests
{
    [Theory]
    [InlineData("The ApplicationName field is required.", "The Application Name field is required.")]
    [InlineData("The field ApplicationName must be a string with a maximum length of 100.", "The field Application Name must be a string with a maximum length of 100.")]
    [InlineData("Please choose a name", "Please choose a name")]
    public void UseTitle_ReplacesPropertyNameInDefaultMessages(string message, string expected)
    {
        Assert.Equal(expected, FieldValidationMessage<string>.UseTitle(message, "ApplicationName", "Application Name"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Title")]
    public void UseTitle_WhenNoDifferentTitle_ReturnsMessage(string? title)
    {
        Assert.Equal("The Title field is required.", FieldValidationMessage<string>.UseTitle("The Title field is required.", "Title", title));
    }
}
