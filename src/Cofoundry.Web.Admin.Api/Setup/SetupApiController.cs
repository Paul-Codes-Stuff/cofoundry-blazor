using Microsoft.AspNetCore.Mvc;

namespace Cofoundry.Web.Admin.Api;

[Area(AdminApiRouteConstants.AreaName)]
[AutoValidateAntiforgeryToken]
public class SetupApiController : Controller
{
    private readonly IApiResponseHelper _apiResponseHelper;

    public SetupApiController(
        IApiResponseHelper apiResponseHelper
        )
    {
        _apiResponseHelper = apiResponseHelper;
    }

    public Task<JsonResult> Post([FromBody] SetupCofoundryCommandDto dto)
    {
        var command = new SetupCofoundryCommand()
        {
            ApplicationName = dto.ApplicationName,
            Email = dto.Email,
            DisplayName = dto.DisplayName,
            Password = dto.Password
        };

        return _apiResponseHelper.RunCommandAsync(command);
    }
}
