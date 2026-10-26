using Cofoundry.Core.Mail;
using Cofoundry.Core.Web;
using Cofoundry.Domain.MailTemplates;
using Cofoundry.Domain.MailTemplates.Internal;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Points the links in the Cofoundry admin user area's emails at the Blazor admin, when
/// <see cref="AdminBlazorSettings.UseForEmailLinks"/> is on; otherwise the emails are Cofoundry's
/// defaults. The domain builds them from <see cref="AdminSettings.DirectoryName"/> (<see cref="CofoundryAdminUserArea"/> sets the
/// account recovery url to the AngularJS admin's MVC reset password page), so the default templates
/// are built and their urls replaced. Cofoundry registers <see cref="IUserMailTemplateBuilder{T}"/>
/// implementations by scanning, so this applies to emails sent from either admin. A site that
/// customises the admin emails with its own builder replaces this one, and keeps the AngularJS links.
/// </summary>
public class AdminBlazorMailTemplateBuilder : IUserMailTemplateBuilder<CofoundryAdminUserArea>
{
    private readonly IDefaultUserMailTemplateBuilder<CofoundryAdminUserArea> _defaultBuilder;
    private readonly IAuthorizedTaskTokenUrlHelper _authorizedTaskTokenUrlHelper;
    private readonly ISiteUrlResolver _siteUrlResolver;
    private readonly AdminBlazorSettings _adminBlazorSettings;

    public AdminBlazorMailTemplateBuilder(
        IDefaultUserMailTemplateBuilder<CofoundryAdminUserArea> defaultBuilder,
        IAuthorizedTaskTokenUrlHelper authorizedTaskTokenUrlHelper,
        ISiteUrlResolver siteUrlResolver,
        AdminBlazorSettings adminBlazorSettings
        )
    {
        _defaultBuilder = defaultBuilder;
        _authorizedTaskTokenUrlHelper = authorizedTaskTokenUrlHelper;
        _siteUrlResolver = siteUrlResolver;
        _adminBlazorSettings = adminBlazorSettings;
    }

    public async Task<IMailTemplate> BuildAccountRecoveryTemplateAsync(IAccountRecoveryTemplateBuilderContext context)
    {
        var template = await _defaultBuilder.BuildAccountRecoveryTemplateAsync(context);
        if (_adminBlazorSettings.UseForEmailLinks && template is AccountRecoveryMailTemplate recoveryTemplate)
        {
            var path = _authorizedTaskTokenUrlHelper.MakeUrl(AdminBlazorRoutes.ResetPassword, context.Token);
            recoveryTemplate.RecoveryUrl = _siteUrlResolver.MakeAbsolute(path);
        }

        return SetSignInUrl(template);
    }

    public async Task<IMailTemplate> BuildNewUserWithTemporaryPasswordTemplateAsync(INewUserWithTemporaryPasswordTemplateBuilderContext context)
    {
        return SetSignInUrl(await _defaultBuilder.BuildNewUserWithTemporaryPasswordTemplateAsync(context));
    }

    public async Task<IMailTemplate> BuildPasswordResetTemplateAsync(IPasswordResetTemplateBuilderContext context)
    {
        return SetSignInUrl(await _defaultBuilder.BuildPasswordResetTemplateAsync(context));
    }

    public async Task<IMailTemplate> BuildAccountVerificationTemplateAsync(IAccountVerificationTemplateBuilderContext context)
    {
        // The admin user area doesn't use account verification, so there's no Blazor page for it.
        return SetSignInUrl(await _defaultBuilder.BuildAccountVerificationTemplateAsync(context));
    }

    public async Task<IMailTemplate> BuildPasswordChangedTemplateAsync(IPasswordChangedTemplateBuilderContext context)
    {
        return SetSignInUrl(await _defaultBuilder.BuildPasswordChangedTemplateAsync(context));
    }

    private IMailTemplate SetSignInUrl(IMailTemplate template)
    {
        if (_adminBlazorSettings.UseForEmailLinks && template is IMailTemplateWithSignInUrl withSignInUrl)
        {
            withSignInUrl.SignInUrl = _siteUrlResolver.MakeAbsolute(AdminBlazorRoutes.Root);
        }

        return template;
    }
}
