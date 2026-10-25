using Cofoundry.Web.Admin.Blazor.Components.Ui;
using Microsoft.AspNetCore.Components.Forms;

namespace Cofoundry.Web.Admin.Blazor.Tests.Components;

public class AdminFormFieldsTests
{
    private sealed class Command
    {
        public string? Title { get; set; }

        public string? UrlPath { get; set; }
    }

    private sealed class Adapter
    {
        public string? Value { get; set; }
    }

    [Theory]
    [InlineData("UrlPath")]
    [InlineData("urlpath")]
    [InlineData("Command.UrlPath")]
    public void Find_MatchesNameOrLastPartOfPath(string propertyName)
    {
        var command = new Command();
        var fields = new AdminFormFields();
        fields.Add(new FieldIdentifier(command, nameof(Command.Title)));
        fields.Add(new FieldIdentifier(command, nameof(Command.UrlPath)));

        var result = fields.Find(propertyName, command);

        Assert.Equal(new FieldIdentifier(command, nameof(Command.UrlPath)), result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Unknown")]
    public void Find_WhenNoMatch_ReturnsNull(string propertyName)
    {
        var command = new Command();
        var fields = new AdminFormFields();
        fields.Add(new FieldIdentifier(command, nameof(Command.Title)));

        Assert.Null(fields.Find(propertyName, command));
    }

    [Fact]
    public void Find_PrefersFieldOnFormModel()
    {
        var command = new Command();
        var dialogModel = new Command();
        var fields = new AdminFormFields();
        fields.Add(new FieldIdentifier(dialogModel, nameof(Command.Title)));
        fields.Add(new FieldIdentifier(command, nameof(Command.Title)));

        Assert.Equal(new FieldIdentifier(command, nameof(Command.Title)), fields.Find("Title", command));
    }

    [Fact]
    public void Find_WhenAmbiguousAndNotOnFormModel_ReturnsNull()
    {
        var fields = new AdminFormFields();
        fields.Add(new FieldIdentifier(new Command(), nameof(Command.Title)));
        fields.Add(new FieldIdentifier(new Command(), nameof(Command.Title)));

        Assert.Null(fields.Find("Title", new Command()));
    }

    [Fact]
    public void Find_MatchesAdapterByPropertyName()
    {
        // A data model field binds to an adapter's "Value", and is added under the data model property name.
        var adapter = new Adapter();
        var fields = new AdminFormFields();
        fields.Add(new FieldIdentifier(adapter, nameof(Adapter.Value)), "Author");

        Assert.Equal(new FieldIdentifier(adapter, nameof(Adapter.Value)), fields.Find("Model.Author", new Command()));
        Assert.Null(fields.Find("Value", new Command()));
    }

    [Fact]
    public void Remove_RemovesFieldUnderAnyName()
    {
        var adapter = new Adapter();
        var field = new FieldIdentifier(adapter, nameof(Adapter.Value));
        var fields = new AdminFormFields();
        fields.Add(field, "Author");
        fields.Add(field, "Author");

        fields.Remove(field);

        Assert.Null(fields.Find("Author", null));
    }
}
