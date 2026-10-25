using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Components.Forms;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// Values shared by every field in a <see cref="DataModelFieldSet"/>, the Blazor equivalent of the
/// AngularJS cms-additional-parameters (e.g. pickers that filter by the entity's locale).
/// </summary>
public sealed record DataModelFieldContext(int? LocaleId);

/// <summary>
/// One property of a dynamic data model (custom entity, page block or nested data model), bound to the
/// real CLR model instance. The schema comes from the domain's <see cref="IDynamicDataModelSchema"/>,
/// so the field sees the same template name, display name and additional attributes as the AngularJS
/// cms-form-dynamic-field-set did, and the model's own data annotations validate it.
/// </summary>
public sealed class DataModelField
{
    private readonly Dictionary<Type, IDataModelFieldBinding> _bindings = [];

    public DataModelField(object model, PropertyInfo property, DynamicDataModelSchemaProperty schema, DataModelFieldContext context)
    {
        Model = model;
        Property = property;
        Schema = schema;
        Context = context;
    }

    public object Model { get; }

    public PropertyInfo Property { get; }

    public DynamicDataModelSchemaProperty Schema { get; }

    public DataModelFieldContext Context { get; }

    public string Title => Schema.DisplayName;

    public string? Description => Schema.Description;

    /// <summary>
    /// Set by <c>[ReadOnly]</c>: the value is shown but cannot be edited.
    /// </summary>
    public bool IsReadOnly => GetAttribute<bool>(DataModelAttributes.ReadOnly);

    /// <summary>
    /// As in the AngularJS field set, a read-only field is never required.
    /// </summary>
    public bool IsRequired => Schema.IsRequired && !IsReadOnly;

    /// <summary>
    /// The property type with any <see cref="Nullable{T}"/> removed.
    /// </summary>
    public Type ValueType => Nullable.GetUnderlyingType(Property.PropertyType) ?? Property.PropertyType;

    public IReadOnlyCollection<IDataModelFieldBinding> Bindings => _bindings.Values;

    public bool HasAttribute(string key) => FindAttribute(key, out _);

    /// <summary>
    /// Reads one of the schema's additional attributes (e.g. "Maxlength", "Rows", "MinWidth"), ignoring
    /// the key's case and converting the value to <typeparamref name="T"/>. Returns the default when the
    /// attribute is missing or cannot be converted.
    /// </summary>
    public T? GetAttribute<T>(string key)
    {
        if (!FindAttribute(key, out var value))
        {
            return default;
        }

        try
        {
            return (T?)DataModelValueConverter.Convert(value, typeof(T));
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return default;
        }
    }

    /// <summary>
    /// Returns a binding that reads and writes the property as <typeparamref name="T"/>, so a field
    /// component can use an existing form field (e.g. <c>FormFieldImageAsset</c>, which takes an
    /// <c>int?</c>) whatever the property's own type is (e.g. <c>int</c>).
    /// </summary>
    /// <param name="emptyWhenDefault">
    /// Treat the type's default value as empty: an entity id of 0 shows as "nothing selected" and fails
    /// a required check.
    /// </param>
    public DataModelFieldBinding<T> Bind<T>(bool emptyWhenDefault = false)
    {
        if (!_bindings.TryGetValue(typeof(T), out var binding))
        {
            binding = new DataModelFieldBinding<T>(this, emptyWhenDefault);
            _bindings.Add(typeof(T), binding);
        }

        return (DataModelFieldBinding<T>)binding;
    }

    private bool FindAttribute(string key, out object? value)
    {
        foreach (var attribute in Schema.AdditionalAttributes)
        {
            if (string.Equals(attribute.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = attribute.Value;
                return true;
            }
        }

        value = null;
        return false;
    }
}

/// <summary>
/// The validation side of a <see cref="DataModelFieldBinding{T}"/>, used by <see cref="DataModelFieldSet"/>.
/// </summary>
public interface IDataModelFieldBinding
{
    FieldIdentifier FieldIdentifier { get; }

    bool IsEmpty { get; }
}

/// <summary>
/// Adapts a data model property to a <typeparamref name="T"/> value. Form fields bind to
/// <see cref="Value"/> (e.g. <c>@bind-Value="binding.Value"</c>), so their validation messages are keyed
/// on this object, and <see cref="DataModelFieldSet"/> files the model's validation results here.
/// </summary>
public sealed class DataModelFieldBinding<T> : IDataModelFieldBinding
{
    private readonly DataModelField _field;
    private readonly bool _emptyWhenDefault;

    internal DataModelFieldBinding(DataModelField field, bool emptyWhenDefault)
    {
        _field = field;
        _emptyWhenDefault = emptyWhenDefault;
    }

    public T? Value
    {
        get
        {
            var value = _field.Property.GetValue(_field.Model);

            if (_emptyWhenDefault && IsDefault(value))
            {
                value = null;
            }

            return (T?)DataModelValueConverter.Convert(value, typeof(T));
        }
        set => _field.Property.SetValue(_field.Model, DataModelValueConverter.Convert(value, _field.Property.PropertyType));
    }

    public FieldIdentifier FieldIdentifier => new(this, nameof(Value));

    public bool IsEmpty
    {
        get
        {
            var value = _field.Property.GetValue(_field.Model);

            return value == null
                || (value is string text && string.IsNullOrWhiteSpace(text))
                || (_emptyWhenDefault && IsDefault(value));
        }
    }

    private bool IsDefault(object? value)
    {
        return value != null
            && _field.ValueType.IsValueType
            && value.Equals(Activator.CreateInstance(_field.ValueType));
    }
}

internal static class DataModelValueConverter
{
    /// <summary>
    /// Converts between the property type and the type a form field works with, e.g. int and int?,
    /// or int and decimal. Null becomes the default of a non-nullable value type.
    /// </summary>
    public static object? Convert(object? value, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType);

        if (value == null)
        {
            return targetType.IsValueType && underlyingType == null ? Activator.CreateInstance(targetType) : null;
        }

        var type = underlyingType ?? targetType;

        if (type.IsInstanceOfType(value))
        {
            return value;
        }

        if (type.IsEnum)
        {
            return value is string name ? Enum.Parse(type, name, true) : Enum.ToObject(type, value);
        }

        return System.Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
    }
}
