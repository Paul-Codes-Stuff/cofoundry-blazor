using Cofoundry.Web.Admin.Blazor.Components.Ui;

namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Loads the reference data that admin forms and filters need for their dropdowns (directories,
/// locales, page templates, routing rules). This replaces the AngularJS self-loading selector
/// directives: pages load what they need up front so they can show a single loading state.
/// </summary>
public class AdminLookups
{
    // Matches the maximum page size the AngularJS admin's API allowed for these lookups.
    private const int MaxLookupPageSize = 100;

    private readonly IAdminBlazorExecutor _executor;

    public AdminLookups(IAdminBlazorExecutor executor)
    {
        _executor = executor;
    }

    /// <summary>
    /// Every page directory as a dropdown option labelled with its full url path, in path order.
    /// </summary>
    public async Task<IReadOnlyList<SelectOption<int>>> GetDirectoryOptionsAsync()
    {
        var root = await _executor.QueryAsync(new GetPageDirectoryTreeQuery());

        return root
            .Flatten()
            .OrderBy(d => d.FullUrlPath, StringComparer.OrdinalIgnoreCase)
            .Select(d => new SelectOption<int>(d.PageDirectoryId, d.FullUrlPath))
            .ToList();
    }

    public async Task<IReadOnlyCollection<ActiveLocale>> GetLocalesAsync()
    {
        return await _executor.QueryAsync(new GetAllActiveLocalesQuery());
    }

    /// <summary>
    /// Locales as dropdown options labelled "Name (language-tag)". The AngularJS selector is only
    /// shown when more than one locale exists, so callers should check <c>Count &gt; 1</c>.
    /// </summary>
    public async Task<IReadOnlyList<SelectOption<int?>>> GetLocaleOptionsAsync()
    {
        var locales = await GetLocalesAsync();

        return locales
            .Select(l => new SelectOption<int?>(l.LocaleId, $"{l.Name} ({l.IETFLanguageTag})"))
            .ToList();
    }

    public async Task<IReadOnlyList<PageTemplateSummary>> GetPageTemplatesAsync()
    {
        var query = new SearchPageTemplateSummariesQuery
        {
            PageNumber = 1,
            PageSize = MaxLookupPageSize
        };

        var result = await _executor.QueryAsync(query);

        return result.Items.ToList();
    }

    public async Task<IReadOnlyCollection<ICustomEntityRoutingRule>> GetRoutingRulesAsync()
    {
        return await _executor.QueryAsync(new GetAllCustomEntityRoutingRulesQuery());
    }
}
