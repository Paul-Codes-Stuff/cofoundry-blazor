using System.ComponentModel.DataAnnotations;
using Cofoundry.Core.DependencyInjection;
using Cofoundry.Web.Admin.Blazor;

namespace Dev.Sandbox;

/// <summary>
/// Adds the Products module's Blazor page. The module is described here as well as in the AngularJS
/// registration (ProductsModuleRegistration), so it also works without the AngularJS admin (a
/// BlazorAdminOnly build); when both are installed the AngularJS description is used and only the nav link
/// moves to Blazor.
/// </summary>
public class ProductsBlazorModuleRegistration : IAdminBlazorModuleRegistration
{
    public IEnumerable<AdminBlazorModule> GetModules()
    {
        yield return new("BTPPRD", typeof(ProductDetailsPage))
        {
            Module = new AdminModule()
            {
                AdminModuleCode = "BTPPRD",
                Title = "Products",
                Description = "Testing module.",
                MenuCategory = AdminModuleMenuCategory.ManageSite,
                PrimaryOrdering = AdminModuleMenuPrimaryOrdering.Tertiary
            }
        };
    }
}

/// <summary>
/// The Products module is a demo with no database: this keeps the one product in memory so the page has
/// something to load and save.
/// </summary>
public class ProductStore
{
    private readonly object _lock = new();
    private ProductDetails _product = new("Test Product", "TEST01", null);

    public ProductDetails Get()
    {
        lock (_lock)
        {
            return _product;
        }
    }

    public void Update(UpdateProductCommand command)
    {
        lock (_lock)
        {
            _product = new ProductDetails(command.Title, command.Ref, command.Test);
        }
    }
}

public sealed record ProductDetails(string Title, string Ref, int? Test);

public class UpdateProductCommand
{
    [Required, MaxLength(32)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(32)]
    public string Ref { get; set; } = string.Empty;

    public int? Test { get; set; }
}

public class ProductsDependencyRegistration : IDependencyRegistration
{
    public void Register(IContainerRegister container)
    {
        container.Register<ProductStore>(RegistrationOptions.SingletonScope());
    }
}
