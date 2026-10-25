using Cofoundry.Core.ResourceFiles;
using Microsoft.Extensions.DependencyInjection;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Content for the admin dashboard. Mirrors <c>DashboardContentProvider</c> in the AngularJS admin
/// project: a site can supply its own html at <see cref="CustomContentPath"/>, otherwise a
/// welcome message with the application name is shown.
/// </summary>
/// <param name="CustomHtml">
/// The site's custom dashboard html, or <see langword="null"/> if there isn't one. This is a
/// developer-authored file, so it is rendered as markup.
/// </param>
/// <param name="ApplicationName">
/// Used for the default message. It is an admin-editable setting, so it is rendered as text rather
/// than concatenated into html as the AngularJS provider does.
/// </param>
public sealed record DashboardContent(string? CustomHtml, string? ApplicationName)
{
    public const string CustomContentPath = "/Cofoundry/Admin/Dashboard/Dashboard.html";

    public static async Task<DashboardContent> LoadAsync(IAdminBlazorExecutor executor)
    {
        var customHtml = await executor.WithServicesAsync(async services =>
        {
            var file = services.GetRequiredService<IResourceLocator>().GetFile(CustomContentPath);
            if (!file.Exists)
            {
                return null;
            }

            using var stream = file.CreateReadStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        });

        if (customHtml != null)
        {
            return new DashboardContent(customHtml, null);
        }

        var settings = await executor.QueryAsync(new GetSettingsQuery<GeneralSiteSettings>());

        return new DashboardContent(null, settings.ApplicationName);
    }
}
