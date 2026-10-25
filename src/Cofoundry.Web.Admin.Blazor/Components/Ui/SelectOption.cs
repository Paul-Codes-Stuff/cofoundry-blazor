namespace Cofoundry.Web.Admin.Blazor.Components.Ui;

/// <summary>
/// An option in a <see cref="FormFieldDropdown{TValue}"/>.
/// </summary>
public sealed record SelectOption<TValue>(TValue Value, string Text);
