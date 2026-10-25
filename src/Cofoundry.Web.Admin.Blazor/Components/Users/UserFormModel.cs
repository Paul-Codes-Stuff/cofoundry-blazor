namespace Cofoundry.Web.Admin.Blazor.Components.Users;

/// <summary>
/// The fields of the add and edit user forms. The domain commands can't be bound directly because
/// which fields are required depends on the user area (see <see cref="UserAreaInfo"/>), and adding a
/// user runs a different command when the area allows password sign in.
/// </summary>
public sealed class UserFormModel
{
    // The only constructor, as static SSR form binding (UserEditSelfPage) needs. That page calls
    // ApplyUserArea on the posted model before it's validated.
    public UserFormModel()
    {
    }

    public static UserFormModel Create(UserAreaInfo userArea)
    {
        var model = new UserFormModel();
        model.ApplyUserArea(userArea);

        return model;
    }

    // Private setters, so a form post can't set them.
    public bool IsUsernameRequired { get; private set; }

    public bool IsEmailRequired { get; private set; }

    public void ApplyUserArea(UserAreaInfo userArea)
    {
        IsUsernameRequired = !userArea.UseEmailAsUsername;
        IsEmailRequired = userArea.AllowPasswordSignIn || userArea.UseEmailAsUsername;
    }

    public static UserFormModel FromUser(UserDetails user, UserAreaInfo userArea)
    {
        var model = new UserFormModel()
        {
            DisplayName = user.DisplayName,
            Username = user.Username,
            Email = user.Email,
            RoleId = user.Role.RoleId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RequirePasswordChange = user.RequirePasswordChange,
            IsAccountVerified = user.AccountVerifiedDate.HasValue,
            IsActive = user.AccountStatus == UserAccountStatus.Active
        };
        model.ApplyUserArea(userArea);

        return model;
    }

    public UpdateUserCommand ToUpdateCommand(int userId, UserAreaInfo userArea)
    {
        return new UpdateUserCommand
        {
            UserId = userId,
            DisplayName = DisplayName,
            Username = userArea.UseEmailAsUsername ? null : Username,
            Email = Email,
            RoleId = RoleId,
            FirstName = FirstName,
            LastName = LastName,
            RequirePasswordChange = RequirePasswordChange,
            IsAccountVerified = IsAccountVerified,
            IsActive = IsActive
        };
    }

    [StringLength(150)]
    [Display(Name = "Display Name")]
    public string? DisplayName { get; set; }

    [RequiredIf(nameof(IsUsernameRequired))]
    [StringLength(150)]
    public string? Username { get; set; }

    [RequiredIf(nameof(IsEmailRequired))]
    [StringLength(150)]
    [EmailAddress(ErrorMessage = "Please use a valid email address")]
    public string? Email { get; set; }

    [Required]
    [Display(Name = "Role")]
    public int? RoleId { get; set; }

    [StringLength(32)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [StringLength(32)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    public bool RequirePasswordChange { get; set; }

    public bool IsAccountVerified { get; set; }

    public bool IsActive { get; set; } = true;
}
