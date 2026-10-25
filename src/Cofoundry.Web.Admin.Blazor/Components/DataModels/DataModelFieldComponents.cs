using Microsoft.AspNetCore.Components;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// Base class for a component that edits one data model property in a <see cref="DataModelFieldSet"/>.
/// It typically wraps an existing form field (FormFieldText, FormFieldImageAsset...) and maps the
/// field's schema attributes onto it.
/// </summary>
public abstract class DataModelFieldComponentBase : ComponentBase
{
    [Parameter, EditorRequired] public DataModelField Field { get; set; } = default!;
}

/// <summary>
/// Maps data template names to the components that edit them. This is the Blazor admin's extension
/// point for custom field types, the equivalent of an AngularJS plugin adding a cms-form-field-*
/// directive: implement it in any assembly Cofoundry scans and it is picked up automatically.
/// Registrations are applied after the built-in ones, so they can also replace a built-in field.
/// </summary>
public interface IDataModelFieldComponentRegistration
{
    /// <summary>
    /// Template names (the schema's <see cref="DynamicDataModelSchemaProperty.DataTemplateName"/>, e.g.
    /// "ImageAsset", or the TemplateHint/UIHint an attribute sets) and the component for each. Each
    /// component must derive from <see cref="DataModelFieldComponentBase"/>.
    /// </summary>
    IEnumerable<KeyValuePair<string, Type>> GetComponents();
}

/// <summary>
/// The field types the Blazor admin supports so far. Anything else renders
/// <see cref="DataModelUnsupportedField"/>.
/// </summary>
public class DefaultDataModelFieldComponentRegistration : IDataModelFieldComponentRegistration
{
    public IEnumerable<KeyValuePair<string, Type>> GetComponents()
    {
        // Text. Char gets a max length of 1 from DataModelTextField.
        yield return new("String", typeof(DataModelTextField));
        yield return new("Char", typeof(DataModelTextField));
        yield return new("Url", typeof(DataModelTextField));
        yield return new("EmailAddress", typeof(DataModelTextField));
        yield return new("MultilineText", typeof(DataModelTextAreaField));

        yield return new("Boolean", typeof(DataModelCheckboxField));

        // Numbers. The AngularJS admin only mapped the floating point types and [Number]; integers
        // without [Number] rendered nothing there.
        foreach (var name in new[] { "Number", "Int16", "Int32", "Int64", "Byte", "Single", "Double", "Decimal" })
        {
            yield return new(name, typeof(DataModelNumberField));
        }

        yield return new("ImageAsset", typeof(DataModelImageField));
    }
}

/// <summary>
/// Resolves the component for a data template name from every <see cref="IDataModelFieldComponentRegistration"/>.
/// </summary>
public class DataModelFieldComponentRegistry
{
    private readonly Dictionary<string, Type> _components = new(StringComparer.OrdinalIgnoreCase);

    public DataModelFieldComponentRegistry(IEnumerable<IDataModelFieldComponentRegistration> registrations)
    {
        // Built-in first so that other registrations win.
        var ordered = registrations.OrderBy(r => r is DefaultDataModelFieldComponentRegistration ? 0 : 1);

        foreach (var registration in ordered)
        {
            foreach (var (templateName, componentType) in registration.GetComponents())
            {
                if (!typeof(DataModelFieldComponentBase).IsAssignableFrom(componentType))
                {
                    throw new InvalidOperationException(
                        $"{registration.GetType().Name} maps '{templateName}' to {componentType.Name}, which does not derive from {nameof(DataModelFieldComponentBase)}.");
                }

                _components[templateName] = componentType;
            }
        }
    }

    public Type GetComponentType(string dataTemplateName)
    {
        return _components.TryGetValue(dataTemplateName, out var type) ? type : typeof(DataModelUnsupportedField);
    }
}

/// <summary>
/// Keys of the schema's additional attributes, as the domain's metadata attributes and decorators add
/// them. <see cref="DataModelField.GetAttribute{T}(string)"/> ignores case.
/// </summary>
public static class DataModelAttributes
{
    public const string MaxLength = "Maxlength";
    public const string MinLength = "Minlength";
    public const string Min = "Min";
    public const string Max = "Max";
    public const string Step = "Step";
    public const string Rows = "Rows";
    public const string Placeholder = "Placeholder";
    public const string ReadOnly = "readonly";
    public const string PreviewWidth = "PreviewWidth";
    public const string PreviewHeight = "PreviewHeight";
    public const string PreviewTitle = "PreviewTitle";
    public const string PreviewDescription = "PreviewDescription";
    public const string PreviewImage = "PreviewImage";
}
