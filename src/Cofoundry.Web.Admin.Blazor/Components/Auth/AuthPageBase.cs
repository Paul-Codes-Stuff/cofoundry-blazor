using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Http;

namespace Cofoundry.Web.Admin.Blazor.Components.Auth;

/// <summary>
/// Shared set-up for the static SSR auth pages, ported from the AngularJS admin's AuthController.
/// Like that controller, every page sends the user to the setup page until the site is set up.
/// </summary>
public abstract class AuthPageBase : ComponentBase
{
    [Inject] protected IAdvancedContentRepository ContentRepository { get; set; } = default!;

    [Inject] protected NavigationManager Navigation { get; set; } = default!;

    [Inject] protected AdminSettings AdminSettings { get; set; } = default!;

    [CascadingParameter] protected HttpContext HttpContext { get; set; } = default!;

    /// <summary>
    /// The content repository for the Cofoundry admin user area.
    /// </summary>
    protected IAdvancedContentRepository Repository { get; private set; } = default!;

    protected bool IsPost => HttpMethods.IsPost(HttpContext.Request.Method);

    /// <summary>
    /// Redirects to setup if the site isn't set up yet, else runs <see cref="OnAuthPageInitializedAsync"/>.
    /// </summary>
    protected override async Task OnInitializedAsync()
    {
        Repository = ContentRepository.WithContext<CofoundryAdminUserArea>();

        var internalSettings = await Repository.ExecuteQueryAsync(new GetSettingsQuery<InternalSettings>());
        if (!internalSettings.IsSetup)
        {
            Navigation.NavigateTo(AdminBlazorRoutes.Mvc.Setup(AdminSettings.DirectoryName));
            return;
        }

        await OnAuthPageInitializedAsync();
    }

    protected abstract Task OnAuthPageInitializedAsync();

    protected Task<bool> IsSignedInAsync()
    {
        return Repository.Users().Current().IsSignedIn().ExecuteAsync();
    }

    /// <summary>
    /// The description and input attributes (e.g. minlength) of the admin user area's password
    /// policy, as the MVC pages added with the cf-policy tag helper.
    /// </summary>
    protected Task<PasswordPolicyDescription> GetPasswordPolicyAsync()
    {
        return Repository
            .UserAreas()
            .PasswordPolicies()
            .GetByCode(CofoundryAdminUserArea.Code)
            .AsDescription()
            .ExecuteAsync();
    }

    protected static Dictionary<string, object> GetPasswordInputAttributes(PasswordPolicyDescription? policy)
    {
        var attributes = new Dictionary<string, object>();
        if (policy == null)
        {
            return attributes;
        }

        foreach (var attribute in policy.Attributes)
        {
            attributes[attribute.Key] = attribute.Value;
        }

        if (!string.IsNullOrEmpty(policy.Description))
        {
            attributes.TryAdd("title", policy.Description);
        }

        return attributes;
    }

    /// <summary>
    /// Shows a domain validation error under the field it names, if <paramref name="model"/> has that
    /// property, else returns it so the page can show it in the form's summary.
    /// </summary>
    protected static string? AddValidationError(
        ValidationException ex,
        object model,
        ValidationMessageStore messages,
        EditContext editContext,
        string? defaultPropertyName = null
        )
    {
        var message = ex.ValidationResult?.ErrorMessage ?? ex.Message;
        var propertyName = ex.ValidationResult?.MemberNames
            .Select(n => n[(n.LastIndexOf('.') + 1)..])
            .FirstOrDefault(n => model.GetType().GetProperty(n) != null)
            ?? defaultPropertyName;

        if (propertyName == null)
        {
            return message;
        }

        messages.Add(new FieldIdentifier(model, propertyName), message);
        editContext.NotifyValidationStateChanged();

        return null;
    }
}
