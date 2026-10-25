namespace Dev.Sandbox;

/// <summary>
/// A second user area for the sandbox. The admin only offers page and directory access rules,
/// and the per-area users screens, once a site has a user area besides the Cofoundry admin one.
/// There is no member sign in page on the sandbox site, so <see cref="SignInPath"/> is null and a
/// denied request gets the rule's violation action (403 or 404) instead of a redirect.
/// </summary>
public class MemberUserArea : IUserAreaDefinition
{
    public const string Code = "MEM";

    public string UserAreaCode => Code;

    public string Name => "Member";

    public bool AllowPasswordSignIn => true;

    public bool UseEmailAsUsername => true;

    public string? SignInPath => null;

    // The Cofoundry admin area stays the default auth scheme.
    public bool IsDefaultAuthScheme => false;

    public void ConfigureOptions(UserAreaOptions options)
    {
    }
}
