# Cofoundry.Web.Admin.Api

The Cofoundry admin panel's JSON API (the `/admin/api` endpoints), without the AngularJS admin panel. It's a
copy of the controllers in `Cofoundry.Web.Admin` (`Admin/Api`), so that code which calls the API keeps
working once `Cofoundry.Web.Admin` is removed:

* admin modules and plugins whose own controllers derive from `BaseAdminApiController`
* scripts that call the API with the admin's cookie

The Blazor admin (`Cofoundry.Web.Admin.Blazor`) doesn't need it: it calls the domain in-process.

## Installing

Reference this project from your website. Its route, startup and DI registrations are discovered
automatically, so no `Program.cs` code is needed.

* **Without `Cofoundry.Web.Admin`:** the API is served where it always was, `/{Cofoundry:Admin:DirectoryName}/api`
  (`/admin/api` by default).
* **Next to `Cofoundry.Web.Admin`:** that package serves its own copy at the same path, so set a different
  `BaseUrl` (below). Startup stops with an error if they're the same.

## Settings

In the `Cofoundry:AdminApi` configuration section (`AdminApiSettings`):

| Setting | Default | |
| --- | --- | --- |
| `BaseUrl` | `{Cofoundry:Admin:DirectoryName}/api` | The path the API is served under, without leading or trailing slashes, e.g. `admin/api`. Letters, numbers and dashes, with slashes between segments. |
| `Disabled` | `false` | Removes the API's routes. They're also removed when the admin panel is disabled (`Cofoundry:Admin:Disabled`). |

```json
{
  "Cofoundry": {
    "AdminApi": {
      "BaseUrl": "admin-next/api"
    }
  }
}
```

The Blazor admin leaves `/{its directory}/api` alone, so the API can live under it: the Dev.Sandbox serves
the Blazor admin at `/admin-next` and this API at `/admin-next/api`. Once `Cofoundry.Web.Admin` is removed,
both can use the default `admin` directory.

## Differences from Cofoundry.Web.Admin's copy

The endpoints, request and response shapes, authorization (`[AdminAuthorize]`, the admin user area) and
antiforgery check (`[AutoValidateAntiforgeryToken]`) are the same. The base url is the only difference a
client sees.

* **Namespace** `Cofoundry.Web.Admin.Api`. The public types keep their names (`BaseAdminApiController`,
  `ForAdminApiController`, `ApiPagingHelper`, `AdminAuthorizeAttribute`, `IDashboardContentProvider`, the
  `*ApiController`s), so moving a plugin over means changing the package reference and the `using`.
* **MVC area** `CofoundryAdminApi` (`AdminApiRouteConstants.AreaName`), not `CofoundryAdmin`. Conventional
  routes pick their controller by area and name, so the two copies of e.g. `PagesApiController` can both be
  installed and each route reaches its own. `BaseAdminApiController` sets the area, so derived controllers get
  it. A controller that doesn't derive from it (like `SetupApiController`) needs
  `[Area(AdminApiRouteConstants.AreaName)]`.
* **The Visual Editor's result filter is removed from its actions.** While `Cofoundry.Web.Admin` is
  installed, its global `VisualEditorContentFilter` wraps every MVC result outside its own directory and
  `/api` to inject the editor, and writes json back with a UTF-8 byte order mark. Its exclusion rules are an
  interface in `Cofoundry.Web.Admin`, so `AdminApiActionDescriptorProvider` removes the filter by type name.

## Moving a plugin's API controllers over

```csharp
using Cofoundry.Web.Admin.Api; // was Cofoundry.Web.Admin

public class ErrorsApiController : BaseAdminApiController { ... }

public class ErrorLoggingApiRouteRegistration : IOrderedRouteRegistration
{
    public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder
            .ForAdminApiController<ErrorsApiController>("plugins/errors")
            .MapGet();
    }
}
```

Its routes then follow `Cofoundry:AdminApi:BaseUrl`. A plugin that still ships an AngularJS client should keep
its controllers on `Cofoundry.Web.Admin`'s base class for now: that client builds its urls from the AngularJS
admin's directory (`/{DirectoryName}/api/plugins/`), which is this API's path only when `Cofoundry.Web.Admin`
isn't installed.
