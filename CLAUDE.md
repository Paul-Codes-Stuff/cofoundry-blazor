# Cofoundry Blazor admin port

This is a fork of the Cofoundry CMS (.NET). The work on this branch is **porting the admin panel from
AngularJS (`src/Cofoundry.Web.Admin`) to Blazor**, as a new project `src/Cofoundry.Web.Admin.Blazor`.
Read `src/Cofoundry.Web.Admin.Blazor/README.md` first: it has the status, what is ported and the known gaps.

## Decisions already made

- **Hosting:** Blazor *Interactive Server*, prerendering off.
- **Coexistence:** the Blazor admin is a clean replacement but is routed under **`/admin-next`** so both admins
  can be installed side-by-side during the migration. The AngularJS admin stays at `/admin`. Do not modify
  `Cofoundry.Web.Admin` except to read it.
- **Auth:** sign-in/out still goes through the AngularJS admin's MVC `auth` controller (shared for now).
- **Data access:** components call the domain layer in-process through `IAdminBlazorExecutor`
  (`Infrastructure/AdminBlazorExecutor.cs`), not the `/admin/api` controllers. Each call runs in its own DI
  scope seeded with the circuit's signed-in user (`CircuitAwareUserSessionService`), because Cofoundry's
  `WebUserSessionService` reads `HttpContext`, which is unreliable over SignalR.
- **UI:** plain Razor components in `Components/Ui` that emit the same markup as the AngularJS `cms-*`
  directives, so the copied compiled `wwwroot/css/shared.css` applies unchanged. No component library.
- **Dropdown data** is loaded up front by `AdminLookups`, not by self-loading selector components.

## How to port a module

1. Read the AngularJS module under `src/Cofoundry.Web.Admin/Admin/Modules/<Module>/Js` (routes, controllers,
   `.html` templates) and its API controller under `Admin/Api`.
2. Find the domain queries/commands the API controller uses (`src/Cofoundry.Domain`) and call those via the
   executor. Commands carry data annotations, so bind forms to them directly.
3. Add components under `Components/<Module>/`, reusing `Components/Ui`. Add the module's `AdminModuleCode`
   and URL to `PortedModuleUrls` in `Components/Layout/AdminNav.razor`.
4. Check permissions with `IPermissionValidationService` (see `Infrastructure/AdminPermissions.cs`).

## Gotchas (learned from the first build)

- Razor `@page` accepts **string literals only**, so the `/admin-next` prefix is repeated in each page
  component. To change it, update `AdminBlazorRoutes.Prefix` and every `@page` directive.
- **Never name a Razor variable `page`** in markup: `@page.X` is parsed as the `@page` directive.
- **Component attributes cannot mix text and C#.** `class="a @b"` works on HTML elements but fails on
  components (RZ9986). Compute the whole value in one expression instead.
- Pages under `Components/Pages` get `[AdminBlazorAuthorize]` from that folder's `_Imports.razor`. Do not put
  it in the root `_Imports.razor`: it would land on `App.razor` and break the signed-out view.
- Modal CSS keeps dialogs transparent until `modal--show` is added; `ModalDialog` does this after first render.
- **A bare name passed to a `string` component parameter is a literal.** `Value="_command.Title"` sends the
  text "_command.Title"; write `Value="@_command.Title"`. Non-string and generic parameters are C# already.
- **Some `cms-*` directives changed the DOM at runtime** (e.g. `cmsTableContainer` adds `class="table"` to its
  `<table>`). Read the directive's JS, not just its `.html`, and put that markup in the Razor directly.
- **Never use `[Authorize(AuthenticationSchemes = ...)]` on components**: `AuthorizeRouteView` throws. The
  admin scheme lives in the `AdminBlazorAuthorizeAttribute.PolicyName` policy instead.
- Dialogs render through a `SectionOutlet` outside `.content` (see `AdminLayout`), else the nav covers them.
- The host needs `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>` or `blazor.web.js` 404s.

## Status and next steps

- **Ported:** shell/nav, shared UI components, Dashboard (`/admin-next` and `/admin-next/dashboard`), Pages
  module (list, create, details, change URL, duplicate).
- **Builds and runs.** Smoke-tested in `samples/src/Dev/Dev.Sandbox` (http://localhost:5006/admin-next/pages):
  list, filter, create, edit, publish/unpublish, change URL and duplicate all work. Delete not yet exercised.
- **Next:** Images module (unblocks the OpenGraph image picker), then Roles and Users (unblocks page access
  rules). The Visual Editor stays on the MVC site.
- No tests yet.

## Git

- Work on the `blazor-admin-port` branch. `origin` is the user's fork (`Paul-Codes-Stuff/cofoundry-blazor`);
  `upstream` is `cofoundry-cms/cofoundry` and is read-only.
