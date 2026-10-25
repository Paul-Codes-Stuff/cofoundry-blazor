namespace Cofoundry.Web.Admin.Blazor.Components.Dialogs;

/// <summary>
/// Gives each open <see cref="ModalDialog"/> its own level, so a dialog opened from inside another
/// (e.g. the image picker in a nested data model dialog) stacks on top of it, as the AngularJS modal
/// service did. A single section would only show the newest dialog, and the older dialog's content,
/// including the field that opened the new one, would be disposed. <see cref="Layout.AdminLayout"/> renders an
/// outlet per level and cascades the stack.
/// </summary>
public sealed class ModalDialogStack
{
    public const int MaxDepth = 5;

    private readonly bool[] _used = new bool[MaxDepth];

    public static string GetSectionName(int level) => $"admin-modals-{level}";

    /// <summary>
    /// Takes the lowest free level. When every level is in use the dialog shares the top one, which
    /// shows only the newest dialog there.
    /// </summary>
    public int Acquire()
    {
        for (var level = 0; level < MaxDepth; level++)
        {
            if (!_used[level])
            {
                _used[level] = true;
                return level;
            }
        }

        return MaxDepth - 1;
    }

    public void Release(int level)
    {
        if (level >= 0 && level < MaxDepth)
        {
            _used[level] = false;
        }
    }
}
