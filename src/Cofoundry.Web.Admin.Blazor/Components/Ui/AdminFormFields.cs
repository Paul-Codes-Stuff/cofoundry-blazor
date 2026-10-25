using Microsoft.AspNetCore.Components.Forms;

namespace Cofoundry.Web.Admin.Blazor.Components.Ui;

/// <summary>
/// The fields rendered in an <see cref="AdminForm"/>, cascaded by the form. Fields add themselves, so
/// <see cref="FormStatus"/> can show a server-side validation error under the field it is about, as the
/// AngularJS fields did through validationErrorService, and only list the errors no field shows.
/// </summary>
public sealed class AdminFormFields
{
    private readonly List<FieldIdentifier> _fields = [];

    public void Add(FieldIdentifier field)
    {
        if (!_fields.Contains(field))
        {
            _fields.Add(field);
        }
    }

    public void Remove(FieldIdentifier field) => _fields.Remove(field);

    /// <summary>
    /// Finds the field an error's property name is about. Names may be paths (e.g. "Command.Title"), so the
    /// last part is compared too, ignoring case. A field on the form's own model wins; otherwise the name
    /// must match only one field, so a message is never filed under a field that doesn't show it.
    /// </summary>
    public FieldIdentifier? Find(string propertyName, object? formModel)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return null;
        }

        var name = propertyName[(propertyName.LastIndexOf('.') + 1)..];
        var matches = _fields
            .Where(f => string.Equals(f.FieldName, propertyName, StringComparison.OrdinalIgnoreCase)
                || string.Equals(f.FieldName, name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var onFormModel = matches.Where(f => ReferenceEquals(f.Model, formModel)).ToList();
        if (onFormModel.Count == 1)
        {
            return onFormModel[0];
        }

        return matches.Count == 1 ? matches[0] : null;
    }
}
