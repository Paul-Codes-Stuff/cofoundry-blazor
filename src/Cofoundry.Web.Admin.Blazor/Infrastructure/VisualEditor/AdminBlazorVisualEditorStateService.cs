using Microsoft.AspNetCore.Http;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Reads the visual editor state (live, preview, edit or a specific version) from the request, as
/// Cofoundry.Web.Admin's AdminVisualEditorStateService does. Cofoundry.Web's default always shows live
/// content, so without this the "?mode=edit" and "?version=" links would do nothing once the AngularJS
/// admin is removed. Registered above that one, which it would otherwise conflict with.
/// </summary>
public class AdminBlazorVisualEditorStateService : IVisualEditorStateService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserContextService _userContextService;
    private readonly ContentSettings _contentSettings;
    private readonly IVisualEditorStateCache _visualEditorStateCache;
    private readonly AdminSettings _adminSettings;

    public AdminBlazorVisualEditorStateService(
        IHttpContextAccessor httpContextAccessor,
        IUserContextService userContextService,
        ContentSettings contentSettings,
        IVisualEditorStateCache visualEditorStateCache,
        AdminSettings adminSettings
        )
    {
        _httpContextAccessor = httpContextAccessor;
        _userContextService = userContextService;
        _contentSettings = contentSettings;
        _visualEditorStateCache = visualEditorStateCache;
        _adminSettings = adminSettings;
    }

    public async Task<VisualEditorState> GetCurrentAsync()
    {
        var state = _visualEditorStateCache.Get();

        // Cached for the request, so no locking is needed.
        if (state == null)
        {
            state = await CreateAsync();
            _visualEditorStateCache.Set(state);
        }

        return state;
    }

    private async Task<VisualEditorState> CreateAsync()
    {
        if (_adminSettings.Disabled)
        {
            return new VisualEditorState();
        }

        var query = _httpContextAccessor.HttpContext?.Request.Query;
        var modeParameter = query?["mode"].ToString();
        var versionId = IntParser.ParseOrNull(query?["version"].ToString());

        var userContext = await _userContextService.GetCurrentContextByUserAreaAsync(CofoundryAdminUserArea.Code);

        // Live for signed-out users. Admins can see drafts too, and pick a mode or a version.
        var mode = VisualEditorMode.Live;
        if (userContext.IsCofoundryUser())
        {
            if (versionId.HasValue)
            {
                mode = VisualEditorMode.SpecificVersion;
            }
            else if (!Enum.TryParse(modeParameter, true, out mode))
            {
                mode = VisualEditorMode.Any;
            }
        }
        else if (_contentSettings.AlwaysShowUnpublishedData)
        {
            // Lets developers run a local site against a live database and see pages that aren't
            // published yet.
            mode = VisualEditorMode.Any;
        }

        return new VisualEditorState(mode);
    }
}
