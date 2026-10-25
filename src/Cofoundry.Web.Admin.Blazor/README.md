# Cofoundry.Web.Admin.Blazor

A Blazor (Interactive Server) port of the AngularJS admin in `Cofoundry.Web.Admin`. It is designed to be
installed **alongside** the AngularJS admin while modules are migrated one at a time.

> **Status: first slice.** Builds cleanly. The Dashboard, Pages (list, filter, create, details/edit, publish,
> unpublish, change URL, duplicate), Images, Documents, Site Settings, Roles and Users modules have been smoke-tested
> in `Dev.Sandbox`.

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
* Shared UI: page layout, buttons, form + fields (text, number, text area, checkbox, dropdown, tags, read-only,
  container, image upload, document upload, image anchor location), table (incl. image thumbnail, tag list,
  created/updated audit cells), pager, status/warning messages, time-ago, audit data, modal + confirm dialog
* Dashboard: at `/admin-next/dashboard` and the `/admin-next` root. It shows the site's
  `/Cofoundry/Admin/Dashboard/Dashboard.html` if one exists, otherwise the application name and a welcome message
* Pages: list (filter, paging, publish), create, details (edit incl. OpenGraph image, publish/unpublish, versions, discard draft,
  copy to draft, delete), change URL, duplicate
* Images: list (filter by tags/width/height, paging), upload, details (edit, replace file, delete), and the
  image picker (`FormFieldImageAsset` + "Find an image" dialog, with upload from inside the dialog)
* Documents: list (filter by tags/extension, paging), upload, details (edit, replace file, delete)
* Site Settings: general + SEO settings (view, edit), clear cache
* Roles: list (filter, paging), create, details (edit title and permissions, delete). The permissions field
  groups permissions by entity and only enables a group's other permissions once its Read permission is ticked
* Users: one list per user area at `/admin-next/users/{UserAreaCode}` (filter by username/email/name/status),
  create (with an emailed temporary password when the area allows password sign in), details (edit,
  deactivate, verify, reset password, delete). Which fields show and are required follows the user area's
  settings (`UserAreaInfo`), as in the AngularJS `UsersModuleOptions`. The nav maps every `COFUSR` + area code
  module to its Blazor list

### File uploads

Uploads go through `InputFile` over the SignalR circuit and are buffered in memory on the server
(`BufferedFileSource`) before the domain command runs, because the domain may read a file more than once. The
limit is 30MB (`BufferedFileSource.MaxFileSize`), matching the request size limit the AngularJS admin's api had.
As in the AngularJS admin, images larger than `ImageAssetsSettings.MaxUploadWidth`/`MaxUploadHeight` are resized
to a JPEG in the browser first (`wwwroot/js/image-upload.js`), and the title defaults to the file name.

## Known gaps

* The image picker is single-select only. Its multi-select mode backs the image (and document) collection
  fields, which only appear in dynamic data-model forms (`[ImageCollection]` on custom entity / page block
  models), so they come with that form. Preset filter restrictions (`cms-width`, `cms-min-width`, ...) aren't
  ported either
* Upload fields have no "Remove" button (only needed for optional uploads, which no ported form has yet) and no
  minimum/exact size check (the AngularJS field's `cms-filter`, used by image pickers)
* Page/directory access rules editor (needs user areas + the shared access editor)
* Visual editor (stays on the MVC site; links use `?mode=edit`)
* Forgot password, reset password (emailed links are built by the MVC route library) and forced password
  change are still the MVC pages
* The footer doesn't show the Cofoundry version, which the AngularJS layout does
* Required-field messages use the command's property name when it has no `[Display]` name (e.g. "The
  ApplicationName field is required."); the AngularJS forms showed their own client-side wording
* Server-side validation messages are shown in the form status area, not against individual fields
* A role can't be deleted while any user references it, including soft-deleted users (a domain rule, the same
  in the AngularJS admin). Move users to another role before deleting them if the role should go too
* No tests yet
