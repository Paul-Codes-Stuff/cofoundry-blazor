using Microsoft.AspNetCore.Mvc;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Sends requests for the site's pages to the Blazor admin's setup page until the site is set up, as
/// Cofoundry.Web.Admin's SetupPageActionFactory does for its own. Cofoundry.Web's default throws, so without
/// this a site with only the Blazor admin fails on every page until someone finds the setup page. Registered
/// just below the AngularJS admin's, so its setup page is still used while it's installed.
/// </summary>
public class AdminBlazorSetupPageActionFactory : ISetupPageActionFactory
{
    private readonly AdminSettings _adminSettings;

    public AdminBlazorSetupPageActionFactory(AdminSettings adminSettings)
    {
        _adminSettings = adminSettings;
    }

    /// <inheritdoc/>
    public ActionResult GetSetupPageAction(Controller controller)
    {
        if (_adminSettings.Disabled)
        {
            throw new Exception("Cofoundry has not been setup. The admin panel is disabled so installation must be done manually.");
        }

        return controller.Redirect(AdminBlazorRoutes.Setup);
    }
}
