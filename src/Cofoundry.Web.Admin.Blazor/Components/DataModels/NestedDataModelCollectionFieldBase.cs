using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace Cofoundry.Web.Admin.Blazor.Components.DataModels;

/// <summary>
/// What the nested data model collection fields share: the list binding, edit mode, the MinItems,
/// MaxItems and Orderable attributes, drag to reorder and the [PreviewImage] thumbnails. Items are
/// never changed in place: every change sets a new list, so the edit dialog can work on a shallow copy.
/// </summary>
public abstract class NestedDataModelCollectionFieldBase<TItem> : DataModelFieldComponentBase, IDisposable
    where TItem : class
{
    private EditContext? _subscribedEditContext;
    private int? _dragIndex;

    [Inject] protected IAdminBlazorExecutor Executor { get; set; } = default!;

    [CascadingParameter] public EditContext? EditContext { get; set; }

    [CascadingParameter(Name = "EditMode")] public bool EditMode { get; set; } = true;

    protected DataModelFieldBinding<IReadOnlyList<TItem>?> Binding { get; private set; } = default!;

    protected IReadOnlyList<TItem> Items => Binding.Value ?? [];

    protected IReadOnlyDictionary<int, ImageAssetRenderDetails> Images { get; private set; } = new Dictionary<int, ImageAssetRenderDetails>();

    protected int MaxItems => Field.GetAttribute<int>(NestedDataModelAttributes.MaxItems);

    protected bool IsOrderable => Field.GetAttribute<bool>(NestedDataModelAttributes.Orderable);

    protected bool ShowInput => EditMode && !Field.IsReadOnly;

    protected bool CanReorder => ShowInput && IsOrderable;

    protected bool CanAdd => ShowInput && (MaxItems <= 0 || Items.Count < MaxItems);

    protected string AreaCssClass => (EditMode ? "editmode " : string.Empty)
        + (EditContext?.GetValidationMessages(Binding.FieldIdentifier).Any() == true ? "invalid" : string.Empty);

    protected override async Task OnParametersSetAsync()
    {
        Binding = Field.Bind<IReadOnlyList<TItem>?>();

        if (!ReferenceEquals(_subscribedEditContext, EditContext))
        {
            Unsubscribe();
            _subscribedEditContext = EditContext;

            if (_subscribedEditContext != null)
            {
                _subscribedEditContext.OnValidationStateChanged += OnValidationStateChanged;
            }
        }

        await LoadSchemaAsync();
        await LoadImagesAsync();
    }

    /// <summary>
    /// Loads the nested model schema(s) the first time the field is shown.
    /// </summary>
    protected abstract Task LoadSchemaAsync();

    /// <summary>
    /// The [PreviewImage] id of an item, if its type has one.
    /// </summary>
    protected abstract int? GetImageId(TItem item);

    protected ImageAssetRenderDetails? GetImage(TItem item)
    {
        return GetImageId(item) is int id ? Images.GetValueOrDefault(id) : null;
    }

    /// <summary>
    /// Sets a new list, refreshes the thumbnails and re-validates the field.
    /// </summary>
    protected async Task SetItemsAsync(List<TItem> items)
    {
        Binding.Value = items;
        await LoadImagesAsync();
        EditContext?.NotifyFieldChanged(Binding.FieldIdentifier);
    }

    protected Task AddAsync(TItem item) => SetItemsAsync([.. Items, item]);

    protected Task ReplaceAsync(TItem oldItem, TItem newItem)
    {
        return SetItemsAsync(Items.Select(i => ReferenceEquals(i, oldItem) ? newItem : i).ToList());
    }

    protected Task RemoveAsync(TItem item)
    {
        return SetItemsAsync(Items.Where(i => !ReferenceEquals(i, item)).ToList());
    }

    protected void DragStart(int index) => _dragIndex = index;

    protected Task DropAsync(int toIndex)
    {
        if (!CanReorder || _dragIndex is not int fromIndex || fromIndex == toIndex)
        {
            return Task.CompletedTask;
        }

        _dragIndex = null;
        var items = Items.ToList();
        var item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(toIndex, item);

        return SetItemsAsync(items);
    }

    private async Task LoadImagesAsync()
    {
        var ids = Items.Select(GetImageId).ToList();

        // Only query when an id isn't loaded yet, so reordering or removing doesn't hit the database.
        if (ids.Any(id => id.HasValue && !Images.ContainsKey(id.Value)))
        {
            Images = await NestedDataModelPreview.LoadImagesAsync(Executor, ids);
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

/// <summary>
/// Additional attribute keys set by [NestedDataModelCollection] and [NestedDataModelMultiTypeCollection].
/// </summary>
public static class NestedDataModelAttributes
{
    public const string MinItems = "MinItems";
    public const string MaxItems = "MaxItems";
    public const string Orderable = "Orderable";
    public const string ModelType = "ModelType";
    public const string ModelTypes = "ModelTypes";
    public const string TitleColumnHeader = "TitleColumnHeader";
    public const string DescriptionColumnHeader = "DescriptionColumnHeader";
    public const string ImageColumnHeader = "ImageColumnHeader";
    public const string TypeColumnHeader = "TypeColumnHeader";
}
