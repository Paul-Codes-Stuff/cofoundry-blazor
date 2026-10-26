namespace Cofoundry.Web.Admin.Api;

public class AdminAuthorizeAttribute : AuthorizeUserAreaAttribute
{
    public AdminAuthorizeAttribute()
        : base(CofoundryAdminUserArea.Code)
    {
    }
}
