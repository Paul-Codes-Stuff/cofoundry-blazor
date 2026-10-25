using System.ComponentModel.DataAnnotations;
using Cofoundry.Core.Validation;

namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

public class CommandValidationErrorsTests
{
    [Fact]
    public void Get_KeepsCommandAndModelPropertyNames()
    {
        var command = new TestCommand { Model = new TestModel() };

        var errors = CommandValidationErrors.Get(command);

        Assert.Contains(errors, e => e.Message == "The Name field is required." && e.Properties.SequenceEqual(["Name"]));
        Assert.Contains(errors, e => e.Message == "The Title field is required." && e.Properties.SequenceEqual(["Title"]));
        Assert.DoesNotContain(errors, e => e.Message.EndsWith("validation failed"));
    }

    [Fact]
    public void Get_FilesNestedItemMessagesUnderTheCollection()
    {
        var command = new TestCommand
        {
            Name = "Valid",
            Model = new TestModel
            {
                Title = "Valid",
                Items = [new TestItem { Heading = "Valid" }, new TestItem(), new TestItem()]
            }
        };

        var errors = CommandValidationErrors.Get(command);

        // Both invalid items have the same problem, so the message is listed once.
        var error = Assert.Single(errors);
        Assert.Equal("The Heading field is required.", error.Message);
        Assert.Equal(["Items"], error.Properties);
    }

    [Fact]
    public void Get_FilesDeeperItemMessagesUnderTheTopCollection()
    {
        var command = new TestCommand
        {
            Name = "Valid",
            Model = new TestModel
            {
                Title = "Valid",
                Items = [new TestItem { Heading = "Valid", Children = [new TestChild()] }]
            }
        };

        var error = Assert.Single(CommandValidationErrors.Get(command));

        Assert.Equal("The Text field is required.", error.Message);
        Assert.Equal(["Items"], error.Properties);
    }

    [Fact]
    public void Get_ReturnsNothingForAValidCommand()
    {
        var command = new TestCommand { Name = "Valid", Model = new TestModel { Title = "Valid" } };

        Assert.Empty(CommandValidationErrors.Get(command));
    }

    private class TestCommand
    {
        [Required]
        public string? Name { get; set; }

        [ValidateObject]
        public TestModel? Model { get; set; }
    }

    private class TestModel
    {
        [Required]
        public string? Title { get; set; }

        [ValidateObject]
        public IReadOnlyCollection<TestItem> Items { get; set; } = [];
    }

    private class TestItem
    {
        [Required]
        public string? Heading { get; set; }

        [ValidateObject]
        public IReadOnlyCollection<TestChild> Children { get; set; } = [];
    }

    private class TestChild
    {
        [Required]
        public string? Text { get; set; }
    }
}
