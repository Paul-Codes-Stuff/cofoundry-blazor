# Cofoundry.Web.Admin.Blazor

A Blazor (Interactive Server) port of the AngularJS admin in `Cofoundry.Web.Admin`. It is designed to be
installed **alongside** the AngularJS admin while modules are migrated one at a time.

> **Status: first slice.** Builds cleanly and the Pages module (list, filter, create, details/edit, publish,
> unpublish, change URL, duplicate) has been smoke-tested in `Dev.Sandbox`. Only the shell and Pages exist.

## Installing side-by-side

Reference this project from your website next to `Cofoundry.Web.Admin`. Cofoundry's startup tasks, route
registrations and DI registrations are discovered automatically, so no extra `Program.cs` code is needed.

The host project must also set `RequiresAspNetWebAssets`. The SDK only serves `_framework/blazor.web.js`
when the host itself contains `.razor` files, so without it the admin renders a blank page:

```xml
<PropertyGroup>
  <RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>
</PropertyGroup>
```

* The Blazor admin lives under **`/admin-next`** (the AngularJS admin stays at `/admin`).
* Sign-in/out is still handled by the AngularJS admin's MVC `auth` controller, which both admins share.
* The nav lists every permitted module. Modules not yet ported link to their AngularJS admin URL; ported ones
  link to the Blazor page (see `PortedModuleUrls` in `Components/Layout/AdminNav.razor`).

### Changing the prefix

Razor `@page` directives only accept string literals, so the prefix is repeated in each page component.
Change `AdminBlazorRoutes.Prefix` **and** every `@page "/admin-next/..."` directive.

## How it differs from the AngularJS admin

| AngularJS admin | Blazor admin |
| --- | --- |
| Browser calls `/admin/api/*` controllers | Components call the domain layer in-process via `IAdminBlazorExecutor` |
| `HttpContext` identifies the user | A per-operation DI scope is seeded with the circuit's signed-in user (`CircuitAwareUserSessionService`) |
| `cms-*` directives | Razor components in `Components/Ui`, emitting the same markup so the existing compiled `shared.css` applies |
| Self-loading selector directives | `AdminLookups` loads dropdown data up front |
| `$routeProvider` per module | `@page` routes |

`wwwroot/css` is a copy of the compiled AngularJS admin CSS and assets. Eventually the SCSS should move here.

## Ported so far

* Shell: layout, permitted-module nav, auth
* Shared UI: page layout, buttons, form + fields (text, text area, checkbox, dropdown, tags, read-only, container),
  table, pager, status/warning messages, time-ago, audit data, modal + confirm dialog
* Pages: list (filter, paging, publish), create, details (edit, publish/unpublish, versions, discard draft,
  copy to draft, delete), change URL, duplicate

## Known gaps

* OpenGraph image picker (needs the Images module)
* Page/directory access rules editor (needs user areas + the shared access editor)
* Visual editor (stays on the MVC site; links use `?mode=edit`)
* Server-side validation messages are shown in the form status area, not against individual fields
* No tests yet
