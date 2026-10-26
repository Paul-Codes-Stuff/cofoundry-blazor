namespace Cofoundry.Web.Admin.Blazor;

/// <summary>
/// Describes the built-in admin modules (title, nav menu, ordering, permission), as the AngularJS admin's module
/// registrations do. Cofoundry.Web.Admin owns those registrations, so without it the nav would be empty:
/// <see cref="AdminBlazorModuleDefinitionRegistration"/> adds these instead, for any code no other registration
/// has. Kept in step with <c>Cofoundry.Web.Admin/Admin/Modules/*/Bootstrap</c>, but linking to the Blazor pages.
/// </summary>
public class BuiltInAdminModules
{
    private readonly PagesSettings _pagesSettings;
    private readonly ImageAssetsSettings _imageAssetsSettings;
    private readonly DocumentAssetsSettings _documentAssetsSettings;
    private readonly IEnumerable<ICustomEntityDefinition> _customEntityDefinitions;
    private readonly IUserAreaDefinitionRepository _userAreaDefinitionRepository;

    public BuiltInAdminModules(
        PagesSettings pagesSettings,
        ImageAssetsSettings imageAssetsSettings,
        DocumentAssetsSettings documentAssetsSettings,
        IEnumerable<ICustomEntityDefinition> customEntityDefinitions,
        IUserAreaDefinitionRepository userAreaDefinitionRepository
        )
    {
        _pagesSettings = pagesSettings;
        _imageAssetsSettings = imageAssetsSettings;
        _documentAssetsSettings = documentAssetsSettings;
        _customEntityDefinitions = customEntityDefinitions;
        _userAreaDefinitionRepository = userAreaDefinitionRepository;
    }

    public IEnumerable<AdminModule> GetModules()
    {
        yield return new AdminModule()
        {
            AdminModuleCode = "COFDSH",
            Title = "Dashboard",
            Description = "An overview of your site.",
            MenuCategory = AdminModuleMenuCategory.ManageSite,
            PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Primary,
            SecondaryOrdering = int.MaxValue,
            Url = AdminBlazorRoutes.Dashboard,
            RestrictedToPermission = new DashboardAdminModulePermission()
        };

        if (!_pagesSettings.Disabled)
        {
            yield return new AdminModule()
            {
                AdminModuleCode = "COFPAG",
                Title = "Pages",
                Description = "Manage the pages in your site.",
                MenuCategory = AdminModuleMenuCategory.ManageSite,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Primary,
                Url = AdminBlazorRoutes.Pages,
                RestrictedToPermission = new PageAdminModulePermission()
            };

            yield return new AdminModule()
            {
                AdminModuleCode = "COFDIR",
                Title = "Directories",
                Description = "Manage the directories in your site.",
                MenuCategory = AdminModuleMenuCategory.ManageSite,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Secondry,
                Url = AdminBlazorRoutes.Directories,
                RestrictedToPermission = new PageDirectoryAdminModulePermission()
            };

            yield return new AdminModule()
            {
                AdminModuleCode = "COFPGT",
                Title = "Page Templates",
                Description = "Manage templates for content pages.",
                MenuCategory = AdminModuleMenuCategory.Settings,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Secondry,
                Url = AdminBlazorRoutes.PageTemplates,
                RestrictedToPermission = new PageTemplateAdminModulePermission()
            };
        }

        if (!_imageAssetsSettings.Disabled)
        {
            yield return new AdminModule()
            {
                AdminModuleCode = "COFIMG",
                Title = "Images",
                Description = "Manage the images in your site.",
                MenuCategory = AdminModuleMenuCategory.ManageSite,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Secondry,
                Url = AdminBlazorRoutes.Images,
                RestrictedToPermission = new ImageAssetAdminModulePermission()
            };
        }

        if (!_documentAssetsSettings.Disabled)
        {
            yield return new AdminModule()
            {
                AdminModuleCode = "COFDOC",
                Title = "Documents",
                Description = "Manage the documents in your site.",
                MenuCategory = AdminModuleMenuCategory.ManageSite,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Secondry,
                Url = AdminBlazorRoutes.Documents,
                RestrictedToPermission = new DocumentAssetAdminModulePermission()
            };
        }

        foreach (var definition in _customEntityDefinitions)
        {
            yield return new AdminModule()
            {
                AdminModuleCode = definition.CustomEntityDefinitionCode,
                Title = definition.NamePlural,
                Description = definition.Description,
                MenuCategory = AdminModuleMenuCategory.ManageSite,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Tertiary,
                Url = AdminBlazorRoutes.CustomEntities(definition.CustomEntityDefinitionCode),
                RestrictedToPermission = new CustomEntityAdminModulePermission(definition)
            };
        }

        yield return new AdminModule()
        {
            AdminModuleCode = "COFSET",
            Title = "Site Settings",
            Description = "Manage site settings.",
            MenuCategory = AdminModuleMenuCategory.Settings,
            PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Primary,
            Url = AdminBlazorRoutes.Settings,
            RestrictedToPermission = new SettingsAdminModulePermission()
        };

        yield return new AdminModule()
        {
            AdminModuleCode = "COFROL",
            Title = "Roles & Permissions",
            Description = "Manage user roles and permissions.",
            MenuCategory = AdminModuleMenuCategory.Settings,
            PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Secondry,
            Url = AdminBlazorRoutes.Roles,
            RestrictedToPermission = new RoleAdminModulePermission()
        };

        foreach (var userArea in _userAreaDefinitionRepository.GetAll())
        {
            yield return new AdminModule()
            {
                AdminModuleCode = "COFUSR" + userArea.UserAreaCode,
                Title = userArea.Name + " Users",
                Description = "Manage users in the " + userArea.Name + " user area.",
                MenuCategory = AdminModuleMenuCategory.Settings,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Secondry,
                Url = AdminBlazorRoutes.Users(userArea.UserAreaCode),
                RestrictedToPermission = userArea is CofoundryAdminUserArea
                    ? new CofoundryUserAdminModulePermission()
                    : new NonCofoundryUserAdminModulePermission()
            };
        }

        yield return new AdminModule()
        {
            AdminModuleCode = "COFACC",
            Title = "My Account",
            Description = "Manage your user account.",
            MenuCategory = AdminModuleMenuCategory.Settings,
            PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Last,
            Url = AdminBlazorRoutes.Account
        };
    }
}
