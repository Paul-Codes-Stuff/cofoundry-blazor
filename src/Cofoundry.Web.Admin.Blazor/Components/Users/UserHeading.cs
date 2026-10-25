namespace Cofoundry.Web.Admin.Blazor.Components.Users;

/// <summary>
/// The heading of a user's page. The AngularJS header showed the first and last name. Admin users have
/// neither, so this falls back to the display name or username.
/// </summary>
public static class UserHeading
{
    public static string Get(UserDetails user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return !string.IsNullOrEmpty(name) ? name : user.DisplayName ?? user.Username;
    }
}
