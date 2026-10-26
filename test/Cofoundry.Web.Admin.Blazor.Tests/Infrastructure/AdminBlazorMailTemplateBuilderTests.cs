using Cofoundry.Core.Mail;
using Cofoundry.Core.Web;
using Cofoundry.Domain;
using Cofoundry.Domain.MailTemplates;
using Cofoundry.Domain.MailTemplates.Internal;
using Microsoft.AspNetCore.Http;

namespace Cofoundry.Web.Admin.Blazor.Tests.Infrastructure;

public class AdminBlazorMailTemplateBuilderTests
{
    private const string DefaultRecoveryUrl = "http://site/admin/auth/account-recovery?t=abc";
    private const string DefaultSignInUrl = "http://site/admin";

    [Fact]
    public async Task UseForEmailLinksOff_KeepsTheDefaultLinks()
    {
        var builder = CreateBuilder(useForEmailLinks: false);

        var recovery = (AccountRecoveryMailTemplate)await builder.BuildAccountRecoveryTemplateAsync(new RecoveryContext());
        var changed = (PasswordChangedMailTemplate)await builder.BuildPasswordChangedTemplateAsync(null!);

        Assert.Equal(DefaultRecoveryUrl, recovery.RecoveryUrl);
        Assert.Equal(DefaultSignInUrl, changed.SignInUrl);
    }

    [Fact]
    public async Task UseForEmailLinksOn_PointsTheLinksAtTheBlazorAdmin()
    {
        var builder = CreateBuilder(useForEmailLinks: true);

        var recovery = (AccountRecoveryMailTemplate)await builder.BuildAccountRecoveryTemplateAsync(new RecoveryContext());
        var changed = (PasswordChangedMailTemplate)await builder.BuildPasswordChangedTemplateAsync(null!);

        Assert.Equal("http://site/admin-next/auth/reset-password?t=abc", recovery.RecoveryUrl);
        Assert.Equal("http://site/admin-next", changed.SignInUrl);
    }

    private static AdminBlazorMailTemplateBuilder CreateBuilder(bool useForEmailLinks)
    {
        return new AdminBlazorMailTemplateBuilder(
            new DefaultBuilder(),
            new TokenUrlHelper(),
            new SiteUrlResolver(),
            new AdminBlazorSettings { UseForEmailLinks = useForEmailLinks });
    }

    private sealed class DefaultBuilder : IDefaultUserMailTemplateBuilder<CofoundryAdminUserArea>
    {
        public Task<IMailTemplate> BuildAccountRecoveryTemplateAsync(IAccountRecoveryTemplateBuilderContext context) =>
            Task.FromResult<IMailTemplate>(new AccountRecoveryMailTemplate { RecoveryUrl = DefaultRecoveryUrl });

        public Task<IMailTemplate> BuildPasswordChangedTemplateAsync(IPasswordChangedTemplateBuilderContext context) =>
            Task.FromResult<IMailTemplate>(new PasswordChangedMailTemplate { SignInUrl = DefaultSignInUrl });

        public Task<IMailTemplate> BuildNewUserWithTemporaryPasswordTemplateAsync(INewUserWithTemporaryPasswordTemplateBuilderContext context) =>
            throw new NotImplementedException();

        public Task<IMailTemplate> BuildPasswordResetTemplateAsync(IPasswordResetTemplateBuilderContext context) =>
            throw new NotImplementedException();

        public Task<IMailTemplate> BuildAccountVerificationTemplateAsync(IAccountVerificationTemplateBuilderContext context) =>
            throw new NotImplementedException();
    }

    private sealed class RecoveryContext : IAccountRecoveryTemplateBuilderContext
    {
        public UserSummary User => UserSummary.Uninitialized;

        public string Token => "abc";

        public string? RecoveryUrlPath => null;

        public Task<AccountRecoveryMailTemplate> BuildDefaultTemplateAsync() => throw new NotImplementedException();
    }

    private sealed class TokenUrlHelper : IAuthorizedTaskTokenUrlHelper
    {
        public string MakeUrl(string baseUrl, string token) => $"{baseUrl}?t={token}";

        public string? ParseTokenFromQuery(IQueryCollection queryCollection) => throw new NotImplementedException();
    }

    private sealed class SiteUrlResolver : ISiteUrlResolver
    {
        public string MakeAbsolute(string? path) => "http://site" + path;

        public string MakeAbsolute(string? path, bool forceSsl) => MakeAbsolute(path);
    }
}
