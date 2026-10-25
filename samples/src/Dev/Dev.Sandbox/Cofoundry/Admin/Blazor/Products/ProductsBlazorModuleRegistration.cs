using System.ComponentModel.DataAnnotations;
using Cofoundry.Core.DependencyInjection;
using Cofoundry.Web.Admin.Blazor;

namespace Dev.Sandbox;

/// <summary>
/// Moves the Products module's nav link to its Blazor page. The module itself (title, menu, ordering)
/// still comes from <see cref="ProductsModuleRegistration"/>, the AngularJS registration.
/// </summary>
public class ProductsBlazorModuleRegistration : IAdminBlazorModuleRegistration
{
    public IEnumerable<AdminBlazorModule> GetModules()
    {
        yield return new("BTPPRD", typeof(ProductDetailsPage));
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
