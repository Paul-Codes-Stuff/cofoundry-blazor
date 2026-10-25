# Cofoundry Blazor admin port

This is a fork of the Cofoundry CMS (.NET). The work on this branch is **porting the admin panel from
AngularJS (`src/Cofoundry.Web.Admin`) to Blazor**, as a new project `src/Cofoundry.Web.Admin.Blazor`.
Read `src/Cofoundry.Web.Admin.Blazor/README.md` first: it has the status, what is ported and the known gaps.

## Decisions already made

- **Hosting:** Blazor *Interactive Server*, prerendering off.
- **Coexistence:** the Blazor admin is a clean replacement but is routed under **`/admin-next`** so both admins
  can be installed side-by-side during the migration. The AngularJS admin stays at `/admin`. Do not modify
  `Cofoundry.Web.Admin` except to read it.
- **Auth:** sign-in/out is ported (`Components/Auth`, `/admin-next/auth/login|logout`). These pages are static
  SSR (`[ExcludeFromInteractiveRouting]`) because they set the auth cookie; `App.razor` picks the render mode
  per page. Forgot/reset/change password still use the MVC `auth` controller's pages.
- **Data access:** components call the domain layer in-process through `IAdminBlazorExecutor`
  (`Infrastructure/AdminBlazorExecutor.cs`), not the `/admin/api` controllers. Each call runs in its own DI
  scope seeded with the circuit's signed-in user (`CircuitAwareUserSessionService`), because Cofoundry's
  `WebUserSessionService` reads `HttpContext`, which is unreliable over SignalR.
- **UI:** plain Razor components in `Components/Ui` that emit the same markup as the AngularJS `cms-*`
  directives, so the copied compiled `wwwroot/css/shared.css` applies unchanged. No component library.
- **Dropdown data** is loaded up front by `AdminLookups`, not by self-loading selector components.

## Backwards compatibility (open, revisit before retiring the AngularJS admin)

The Blazor admin calling the domain directly does not break anything today, because `Cofoundry.Web.Admin` and its
`/admin/api` controllers are still installed. But `/admin/api` is a public surface that other code depends on:

- the **Visual Editor**, which stays on the MVC site and calls the API (`VisualEditor/Js/DataServices/PageBlockService.js`)
- **custom admin modules and plugins** that ship their own controllers deriving from `BaseAdminApiController`
  (sandbox Products module, `Cofoundry.Plugins.ErrorLogging.Admin`, `Cofoundry.Plugins.YouTube.Admin`)
- possibly external scripts using the admin cookie (unknown)

Agreed direction, not yet done:

1. Treat `/admin/api` as a supported surface. **Do not remove it** when the AngularJS UI goes. The likely route is
   moving the API controllers and `BaseAdminApiController` into their own package (e.g. `Cofoundry.Web.Admin.Api`)
   with no AngularJS dependency.
2. Keep the Blazor admin on in-process domain calls. Routing it through the API wouldn't help compatibility.
3. The bigger break is the **AngularJS extension model**: custom modules, custom form-field directives (e.g. the
   YouTube plugin) and shared AngularJS services. Blazor now has its own extension points
   (`IAdminBlazorModuleRegistration` for module pages, `IDataModelFieldComponentRegistration` for field types; see
   "Extending the admin" in the README), proven by the sandbox's Products module and by the Error Logging and
   YouTube plugins' new `*.Admin.Blazor` packages. Other extensions (third-party modules and plugins)
   still need Blazor versions. Until they have them, modules without a Blazor page stay reachable because
   `AdminNav` links them to their AngularJS URLs.

## How to port a module

1. Read the AngularJS module under `src/Cofoundry.Web.Admin/Admin/Modules/<Module>/Js` (routes, controllers,
   `.html` templates) and its API controller under `Admin/Api`.
2. Find the domain queries/commands the API controller uses (`src/Cofoundry.Domain`) and call those via the
   executor. Commands carry data annotations, so bind forms to them directly.
3. Add components under `Components/<Module>/`, reusing `Components/Ui`. Add the module's `AdminModuleCode`
   and landing page to `DefaultAdminBlazorModuleRegistration` (`Bootstrap/`).
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
  There's one outlet per `ModalDialogStack` level, so a dialog opened from inside another dialog's content
  stacks on top. With one outlet only the newest dialog renders and the older dialog's content is disposed.
- The host needs `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>` or `blazor.web.js` 404s.
- Field help text goes through `FieldHelp`, which registers with the enclosing `FormSection` so its "Help"
  button appears and toggles `show-helpers`. Don't hand-write `<p class="help-inline">`: it would stay hidden.
- Uploads: buffer into `BufferedFileSource` (the domain reads files more than once). A field that sets its value
  itself must call `NotifyFieldChanged()`, and a page that changes another field in code (e.g. a title from the
  file name) calls `AdminForm.NotifyFieldChanged`, or a stale "required" message stays visible.

## Status and next steps

- **Ported:** shell/nav, sign in/out, shared UI components, Dashboard (`/admin-next` and `/admin-next/dashboard`),
  Pages (list, create, details, change URL, duplicate), Images and Documents (list, filter, upload, details,
  replace file, delete), Site Settings (edit, clear cache), image picker (OpenGraph image on pages), Roles
  (permissions editor), Users (per user area: list, create, edit, reset password, delete), Directories (list,
  create, details, change URL, delete), page/directory access rules (`Components/AccessRules`) and Page
  Templates (read-only list and details), Custom Entities phases 1 to 4 (list, create, details, versions,
  Change Ordering, with the dynamic data-model form in `Components/DataModels`: every field type except
  `[CustomEntityMultiTypeCollection]`, which only page blocks use. That includes dates/times in the browser's
  time zone (`BrowserTimeZone`), option lists (api sources fetched in the browser), image/document/page
  collections and pickers with preset filters, directory selector, the TinyMCE html editor (AngularJS admin's
  4.4.3 build, `wwwroot/lib/tinymce`), an unsupported-field placeholder, and the
  `IDataModelFieldComponentRegistration` extension point), and the plugins' Blazor packages:
  `Cofoundry.Plugins.ErrorLogging.Admin.Blazor` (error log list and details) and
  `Cofoundry.Plugins.YouTube.Admin.Blazor` (the `[YouTube]` field and picker) and
  `Cofoundry.Plugins.Vimeo.Admin.Blazor` (the `[Vimeo]` field and picker).
- **Builds and runs.** Smoke-tested in `samples/src/Dev/Dev.Sandbox` with the `run-blazor-admin` skill (`smoke`
  for Pages, `assets-step.js` for Images/Documents, `settings-step.js` for Site Settings, `image-picker-step.js` for the picker, `roles-users-step.js` for Roles
  and Users, `access-rules-step.js` for Directories and access rules, `page-templates-step.js` for Page Templates, `custom-entities-step.js`, `custom-entity-pickers-step.js`, `nested-data-models-step.js` and `field-types-step.js` (run with `BROWSER_TZ`, uses the dev-only Field Tests entity `SIMFLD`) for Custom Entities, `plugins-step.js`
  for the Error Logging, YouTube and Vimeo plugins; its Vimeo steps need internet access). Page delete not yet exercised.
- **Dev.Sandbox has a "Member" user area** (`Cofoundry/UserAreas/MemberUserArea.cs`, code `MEM`) so access rules
  and a non-admin users list can be exercised. Access Control is hidden on sites with only the admin area.
- **To revisit:** the modal dialog close icon (top right, `Components/Dialogs/ModalDialog.razor`) doesn't work.
  It looks like a bug carried over from the AngularJS admin, which has the same problem.
- **Extension points:** `IAdminBlazorModuleRegistration` adds module pages from other assemblies (router and
  endpoints pick up their assembly; the nav links to them). The sandbox's Products module
  (`Dev.Sandbox/Cofoundry/Admin/Blazor`) is the example, tested by `extension-module-step.js`. A plugin package
  that can be installed without its AngularJS one describes its module in `AdminBlazorModule.Module`, which is
  only used when no other registration has that code (Cofoundry doesn't merge duplicate module codes).
- **Plugins:** Blazor admin parts are separate `*.Admin.Blazor` projects next to the AngularJS ones (both in the
  solution's plugin folders). Dev.Sandbox references both versions of Error Logging, YouTube and Vimeo, and has a
  `test/error` route that throws so there is an error to log. The plugin installs its own table
  (`CofoundryPlugin.Error`) on startup.
- **Next:** the remaining known gaps in the README. Every plugin with an AngularJS admin part now has a Blazor
  package. Page block editing (and `[CustomEntityMultiTypeCollection]`) only matters
  if the Visual Editor moves; it stays on the MVC site for now.
- No tests yet.

## Git

- Work on the `blazor-admin-port` branch. `origin` is the user's fork (`Paul-Codes-Stuff/cofoundry-blazor`);
  `upstream` is `cofoundry-cms/cofoundry` and is read-only.
