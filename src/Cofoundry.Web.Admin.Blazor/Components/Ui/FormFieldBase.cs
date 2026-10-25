using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Cofoundry.Web.Admin.Blazor.Components.Ui;

/// <summary>
/// Shared behaviour for the admin form field components: the title/description chrome, read-only
/// display when the surrounding <see cref="AdminForm"/> is not in edit mode, and the "invalid"
/// state used by the admin css. The concrete field supplies the input element.
/// </summary>
public abstract class FormFieldBase<TValue> : ComponentBase, IDisposable
{
    private EditContext? _subscribedEditContext;
    private FieldIdentifier _fieldIdentifier;
    private bool _hasFieldIdentifier;

    [CascadingParameter(Name = "EditMode")]
    public bool EditMode { get; set; } = true;

    [CascadingParameter]
    public EditContext? CascadingEditContext { get; set; }

    [Parameter]
    public string? Title { get; set; }

    [Parameter]
    public string? Description { get; set; }

    /// <summary>
    /// Forces the read-only display even when the form is in edit mode.
    /// </summary>
    [Parameter]
    public bool ReadOnly { get; set; }

    [Parameter]
    public bool Disabled { get; set; }

    [Parameter]
    public TValue? Value { get; set; }

    [Parameter]
    public EventCallback<TValue> ValueChanged { get; set; }

    [Parameter]
    public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>
    /// True when the input should be shown, i.e. the form is in edit mode and the field is writable.
    /// </summary>
    protected bool ShowInput => EditMode && !ReadOnly;

    /// <summary>
    /// The element id, derived from the bound model property name.
    /// </summary>
    protected string FieldId => _hasFieldIdentifier ? _fieldIdentifier.FieldName : string.Empty;

    protected bool IsInvalid => _hasFieldIdentifier
        && CascadingEditContext != null
        && CascadingEditContext.GetValidationMessages(_fieldIdentifier).Any();

    protected string AreaCssClass => (EditMode ? "editmode " : string.Empty) + (IsInvalid ? "invalid" : string.Empty);

    protected override void OnParametersSet()
    {
        if (ValueExpression != null)
        {
            _fieldIdentifier = FieldIdentifier.Create(ValueExpression);
            _hasFieldIdentifier = true;
        }

        if (!ReferenceEquals(_subscribedEditContext, CascadingEditContext))
        {
            if (_subscribedEditContext != null)
            {
                _subscribedEditContext.OnValidationStateChanged -= OnValidationStateChanged;
            }

            _subscribedEditContext = CascadingEditContext;

            if (_subscribedEditContext != null)
            {
                _subscribedEditContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }
    }

    /// <summary>
    /// Tells the form the bound value changed so its validation message is refreshed. Fields built on
    /// an <c>Input*</c> component get this for free; fields that set the value themselves call it
    /// after invoking <see cref="ValueChanged"/>.
    /// </summary>
    protected void NotifyFieldChanged()
    {
        if (_hasFieldIdentifier)
        {
            CascadingEditContext?.NotifyFieldChanged(_fieldIdentifier);
        }
    }

    /// <summary>
    /// Override to release a field's own resources when it is removed.
    /// </summary>
    protected virtual void OnDisposing()
    {
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        OnDisposing();

        if (_subscribedEditContext != null)
        {
            _subscribedEditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }

        GC.SuppressFinalize(this);
    }
}
