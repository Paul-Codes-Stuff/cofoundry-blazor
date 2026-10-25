# Cofoundry.Web.Admin.Blazor

A Blazor (Interactive Server) port of the AngularJS admin in `Cofoundry.Web.Admin`. It is designed to be
installed **alongside** the AngularJS admin while modules are migrated one at a time.

> **Status: first slice.** Builds cleanly. The Dashboard and Pages modules (list, filter, create, details/edit,
> publish, unpublish, change URL, duplicate) have been smoke-tested in `Dev.Sandbox`.

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
* Sign-in/out is at `/admin-next/auth/login` and `/logout`. Signed-out requests to `/admin-next/...` are sent
  there; `/admin/...` still uses the MVC login. Forgot/reset/change password still use the MVC pages.
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
* Sign in / sign out (static SSR pages in `Components/Auth`, with the same setup, return-URL and
  password-change redirects as the MVC controller). Unlike the MVC login, "Remember me" is honoured:
  unticked gives a browser-session cookie, ticked a persistent one
* Shared UI: page layout, buttons, form + fields (text, text area, checkbox, dropdown, tags, read-only, container),
  table, pager, status/warning messages, time-ago, audit data, modal + confirm dialog
* Dashboard: at `/admin-next/dashboard` and the `/admin-next` root. It shows the site's
  `/Cofoundry/Admin/Dashboard/Dashboard.html` if one exists, otherwise the application name and a welcome message
* Pages: list (filter, paging, publish), create, details (edit, publish/unpublish, versions, discard draft,
  copy to draft, delete), change URL, duplicate

## Known gaps

* OpenGraph image picker (needs the Images module)
* Page/directory access rules editor (needs user areas + the shared access editor)
* Visual editor (stays on the MVC site; links use `?mode=edit`)
* Forgot password, reset password (emailed links are built by the MVC route library) and forced password
  change are still the MVC pages
* The footer doesn't show the Cofoundry version, which the AngularJS layout does
* Server-side validation messages are shown in the form status area, not against individual fields
* No tests yet
