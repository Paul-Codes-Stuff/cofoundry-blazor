namespace Cofoundry.Web.Admin.Blazor.Components.Users;

/// <summary>
/// The fields of the add and edit user forms. The domain commands can't be bound directly because
/// which fields are required depends on the user area (see <see cref="UserAreaInfo"/>), and adding a
/// user runs a different command when the area allows password sign in.
/// </summary>
public sealed class UserFormModel
{
    public UserFormModel(UserAreaInfo userArea)
    {
        IsUsernameRequired = !userArea.UseEmailAsUsername;
        IsEmailRequired = userArea.AllowPasswordSignIn || userArea.UseEmailAsUsername;
    }

    public bool IsUsernameRequired { get; }

    public bool IsEmailRequired { get; }

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
