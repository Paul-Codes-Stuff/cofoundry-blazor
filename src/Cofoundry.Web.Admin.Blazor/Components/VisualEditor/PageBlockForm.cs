namespace Cofoundry.Web.Admin.Blazor.Components.VisualEditor;

/// <summary>
/// The parts of a page block's add or update command that its form edits. The block's data model is
/// validated by the <see cref="DataModels.DataModelFieldSet"/>; the commands themselves are created on save.
/// </summary>
public class PageBlockForm
{
    public IPageBlockTypeDataModel DataModel { get; set; } = default!;

    public int? PageBlockTypeTemplateId { get; set; }
}

/// <summary>
/// The "addBlock" message from <c>js/visual-editor.js</c>: where to add the block.
/// </summary>
/// <param name="InsertMode">Where in the region to add it.</param>
/// <param name="PageTemplateRegionId">The region to add it to.</param>
/// <param name="PermittedBlockTypes">
/// The file names of the block types the region allows. Empty when it allows any.
/// </param>
/// <param name="AdjacentVersionBlockId">
/// The block to insert before or after, for <see cref="OrderedItemInsertMode.BeforeItem"/> and
/// <see cref="OrderedItemInsertMode.AfterItem"/>.
/// </param>
/// <param name="RegionName">For the dialog's title.</param>
public record VisualEditorAddBlockArgs(
    OrderedItemInsertMode InsertMode,
    int PageTemplateRegionId,
    IReadOnlyList<string> PermittedBlockTypes,
    int? AdjacentVersionBlockId,
    string? RegionName
    );

/// <summary>
/// The "editBlock", "moveBlock" and "deleteBlock" messages from <c>js/visual-editor.js</c>.
/// </summary>
/// <param name="VersionBlockId">
/// A page version block id, or a custom entity version page block id on a custom entity's page.
/// </param>
/// <param name="PageBlockTypeId">The block's type, sent with "editBlock".</param>
/// <param name="IsUp">The direction, for "moveBlock".</param>
public record VisualEditorBlockArgs(int VersionBlockId, int? PageBlockTypeId, bool IsUp);
