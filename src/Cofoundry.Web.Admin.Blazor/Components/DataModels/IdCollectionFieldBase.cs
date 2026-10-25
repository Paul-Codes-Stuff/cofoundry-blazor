using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// What the image, document and page collection fields share: a list of entity ids shown as a table, a
/// picker to add or remove them, a remove button per row and drag to reorder. Orderable collections (all
/// asset collections, as in the AngularJS fields) show the ids' order; others are sorted by <see cref="Sort"/>.
/// </summary>
public abstract class IdCollectionFieldBase<TItem> : DataModelFieldComponentBase, IDisposable
    where TItem : class
{
    private EditContext? _subscribedEditContext;
    private IReadOnlyDictionary<int, TItem> _items = new Dictionary<int, TItem>();
    private int? _dragIndex;

    [Inject] protected IAdminBlazorExecutor Executor { get; set; } = default!;

    [CascadingParameter] public EditContext? EditContext { get; set; }

    [CascadingParameter(Name = "EditMode")] public bool EditMode { get; set; } = true;

    protected DataModelFieldBinding<IReadOnlyList<int>?> Binding { get; private set; } = default!;

    protected IReadOnlyList<int> Ids => Binding.Value ?? [];

    /// <summary>
    /// The items in display order. Ids of deleted entities are skipped.
    /// </summary>
    protected IReadOnlyList<TItem> Items
    {
        get
        {
            var items = Ids.Select(id => _items.GetValueOrDefault(id)).OfType<TItem>();
            return (IsOrderable ? items : Sort(items)).ToList();
        }
    }

    protected virtual bool IsOrderable => true;

    protected bool CanReorder => ShowInput && IsOrderable;

    protected bool IsLoading { get; private set; }

    protected bool IsPickerOpen { get; set; }

    protected bool ShowInput => EditMode && !Field.IsReadOnly;

    protected string AreaCssClass => (EditMode ? "editmode " : string.Empty)
        + (EditContext?.GetValidationMessages(Binding.FieldIdentifier).Any() == true ? "invalid" : string.Empty);

    protected override async Task OnParametersSetAsync()
    {
        Binding = Field.Bind<IReadOnlyList<int>?>();

        if (!ReferenceEquals(_subscribedEditContext, EditContext))
        {
            Unsubscribe();
            _subscribedEditContext = EditContext;

            if (_subscribedEditContext != null)
            {
                _subscribedEditContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }

        await LoadItemsAsync();
    }

    protected abstract Task<IReadOnlyDictionary<int, TItem>> QueryAsync(IReadOnlyCollection<int> ids);

    protected abstract int GetId(TItem item);

    /// <summary>
    /// The display order of a collection that isn't orderable.
    /// </summary>
    protected virtual IEnumerable<TItem> Sort(IEnumerable<TItem> items) => items;

    protected async Task OnPickedAsync(IReadOnlyList<int> ids)
    {
        IsPickerOpen = false;
        await SetIdsAsync(ids.ToList());
    }

    protected Task RemoveAsync(TItem item)
    {
        var id = GetId(item);
        return SetIdsAsync(Ids.Where(i => i != id).ToList());
    }

    protected void DragStart(int index) => _dragIndex = index;

    /// <summary>
    /// Moves the dragged row to <paramref name="toIndex"/>, an index into <see cref="Items"/>.
    /// </summary>
    protected Task DropAsync(int toIndex)
    {
        if (!CanReorder || _dragIndex is not int fromIndex || fromIndex == toIndex)
        {
            return Task.CompletedTask;
        }

        _dragIndex = null;
        var ids = Items.Select(GetId).ToList();
        var id = ids[fromIndex];
        ids.RemoveAt(fromIndex);
        ids.Insert(toIndex, id);

        return SetIdsAsync(ids);
    }

    private async Task SetIdsAsync(List<int> ids)
    {
        Binding.Value = ids;
        await LoadItemsAsync();

        // An unordered collection stores its ids in display order, as the AngularJS fields did.
        if (!IsOrderable)
        {
            Binding.Value = Items.Select(GetId).ToList();
        }

        EditContext?.NotifyFieldChanged(Binding.FieldIdentifier);
    }

    /// <summary>
    /// Only queries for ids that aren't loaded yet, so reordering or removing doesn't hit the database.
    /// </summary>
    private async Task LoadItemsAsync()
    {
        if (Ids.All(_items.ContainsKey))
        {
            return;
        }

        IsLoading = true;
        try
        {
            _items = await QueryAsync(Ids.Distinct().ToList());
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }

    private void Unsubscribe()
    {
        if (_subscribedEditContext != null)
        {
            _subscribedEditContext.OnValidationStateChanged -= OnValidationStateChanged;
        }
    }

    public void Dispose() => Unsubscribe();
}
