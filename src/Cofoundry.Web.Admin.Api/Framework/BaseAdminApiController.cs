using Microsoft.AspNetCore.Mvc;

namespace Cofoundry.Web.Admin.Api;

[Area(AdminApiRouteConstants.AreaName)]
[AdminAuthorize]
[AutoValidateAntiforgeryToken]
public class BaseAdminApiController : ControllerBase
{
}
