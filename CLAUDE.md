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
  per page. Forgot/reset/change password are ported too (same folder, static SSR, `AuthPageBase`).
  `AdminBlazorMailTemplateBuilder` points the admin emails' reset and sign-in links at the Blazor admin, for mail
  sent from either admin, only when `Cofoundry:AdminBlazor:UseForEmailLinks` is on (off by default; the domain
  otherwise builds them from `AdminSettings.DirectoryName`). Dev.Sandbox turns it on.
- **Prefix:** `/admin-next` is the default of `Cofoundry:AdminBlazor:DirectoryName` (`AdminBlazorSettings`).
  My Account (`Components/Account`) and first-run setup (`Components/Setup`, `/admin-next/setup`) are static SSR
  too: changing your own email or password updates your security stamp and the domain re-issues the cookie.
- **A circuit can't set cookies.** `CircuitAwareUserSessionService.RefreshAsync` skips the refresh inside a
  circuit (it used to throw "Headers are read-only" after saving). So anything that changes the signed-in
  user's security stamp (email, username, password, "Require Password Change", verification) must be on a
  static SSR page: "Edit" on your own user opens `UserEditSelfPage`, an SSR form sharing `UserFormFields`. A circuit also keeps its user, so
  `js/session-check.js` checks `/admin-next/auth/session` when the tab is shown and reloads if signed out.
- **Data access:** components call the domain layer in-process through `IAdminBlazorExecutor`
  (`Infrastructure/AdminBlazorExecutor.cs`), not the `/admin/api` controllers. Each call runs in its own DI
  scope seeded with the circuit's signed-in user (`CircuitAwareUserSessionService`), because Cofoundry's
  `WebUserSessionService` reads `HttpContext`, which is unreliable over SignalR.
- **UI:** plain Razor components in `Components/Ui` that emit the same markup as the AngularJS `cms-*`
  directives, so the AngularJS admin's styles apply unchanged. No component library. Their SCSS now lives in
  `Styles` (compiled to `wwwroot/css/admin.css` with `npm run build` there; commit both, see "Styles" in the
  README). Check a style change with the skill's `styles-step.js`: baseline first, then compare.
- **Dropdown data** is loaded up front by `AdminLookups`, not by self-loading selector components.
- **Works with the published packages:** the Blazor admin, `Cofoundry.Web.Admin.Api` and the plugins'
  `*.Admin.Blazor` packages must run on the released Cofoundry.Core, Cofoundry.Domain and Cofoundry.Web packages
  (0.13.2 so far), so a site can add them without upgrading Cofoundry. **Do not change `Cofoundry.Core`,
  `Cofoundry.Domain`, `Cofoundry.Web`, `eng/Base.props` or the plugins' non-Blazor projects.** Anything the admin
  needs goes in the admin, even if a core change would be neater. `nuget-compat-check.js` in the skill checks
  both halves (no source changes since the release's commit, and a build against the packages); run it before
  committing anything that touches those projects or the new projects' references, and against each new release.

## Backwards compatibility (open, revisit before retiring the AngularJS admin)

The Blazor admin calling the domain directly does not break anything today, because `Cofoundry.Web.Admin` and its
`/admin/api` controllers are still installed. But `/admin/api` is a public surface that other code depends on:

- the AngularJS **Visual Editor**, which calls the API (`VisualEditor/Js/DataServices/PageBlockService.js`). It is
  part of `Cofoundry.Web.Admin` (`Admin/Modules/VisualEditor`, an AngularJS app plus a global MVC result filter),
  so it goes with that package. **Ported:** the Blazor package has its own editor, which calls the domain
  directly (`Cofoundry:AdminBlazor:UseVisualEditor`, see "Status")
- **custom admin modules and plugins** that ship their own controllers deriving from `BaseAdminApiController`
  (sandbox Products module, `Cofoundry.Plugins.ErrorLogging.Admin`, `Cofoundry.Plugins.YouTube.Admin`)
- possibly external scripts using the admin cookie (unknown)

Agreed direction:

1. Treat `/admin/api` as a supported surface. **Do not remove it** when the AngularJS UI goes. **Done as a copy:**
   `src/Cofoundry.Web.Admin.Api` (read its README) has the API controllers, model binders, `BaseAdminApiController`
   and `ForAdminApiController`, with no AngularJS dependency and `Cofoundry.Web.Admin` untouched. Its base url is
   `Cofoundry:AdminApi:BaseUrl` (default `{Cofoundry:Admin:DirectoryName}/api`; the sandbox uses `admin-next/api`).
   The plugins' controllers still derive from `Cofoundry.Web.Admin`'s base class, because their AngularJS clients
   build urls from the AngularJS admin's directory.
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

- Razor `@page` accepts **string literals only**, so pages declare routes **without** the prefix (`@page "/pages"`).
  `AdminBlazorStartupConfigurationTask` maps the admin in an `app.Map(prefix)` branch (prefix = `PathBase`;
  a `MapGroup` breaks static SSR pages) and `App.razor` sets `<base href>` to it. Build urls with `AdminBlazorRoutes` (properties, not constants: the prefix is set at startup), and keep
  asset urls absolute, since relative ones resolve under the base href.
- **Never name a Razor variable `page`** in markup: `@page.X` is parsed as the `@page` directive.
- Form fields show messages with `FieldValidationMessage` (the field's label replaces the property name in
  DataAnnotations messages), not `ValidationMessage`. Browser steps match the label: "The Application Name
  field is required.", not "ApplicationName".
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
- **Static SSR pages** (`[ExcludeFromInteractiveRouting]`) get no circuit: buttons with `@onclick` do nothing.
  Use links (e.g. "Edit" is `?edit=true`) and a form post. Give `AdminForm` a `FormName` and bind its model to
  a `[SupplyParameterFromForm]` property, which a GET leaves null. Inputs post under the name of the
  expression they're bound to: fields bound to a child component's `Model` parameter post as `Model.X`, so set
  the attribute's `Name = "Model"`, or the post binds nothing (and `??=` would quietly save the stored values).
  The model needs a single parameterless constructor.
- Uploads: buffer into `BufferedFileSource` (the domain reads files more than once). A field that sets its value
  itself must call `NotifyFieldChanged()`, and a page that changes another field in code (e.g. a title from the
  file name) calls `AdminForm.NotifyFieldChanged`, or a stale "required" message stays visible.
- **A field hidden by a condition still validates.** `AdminForm` validates the whole command, including a
  hidden field's value, and that field's message is hidden with it, so Save silently does nothing. Clear the
  value when the field is hidden (e.g. `AddPage` clears `UrlPath` for custom entity details pages).
- **Visual editor steps:** `c.shot` takes full page screenshots, which resize the window, and the editor redraws
  its overlays 500ms after a resize; hover a block from outside it, after a pause. The `data-cms-*` attributes
  only exist in edit mode, and `?version=` of the live version redirects to `?mode=live`.
- **`Cofoundry.Web.Admin`'s Visual Editor filter wraps every MVC result** outside `/{Admin:DirectoryName}` and
  `/api` (a global `VisualEditorContentFilter`) and rewrites it with a UTF-8 BOM. Any MVC endpoint the port adds
  outside those paths gets it: `Cofoundry.Web.Admin.Api` removes it from its actions
  (`AdminApiActionDescriptorProvider`). Blazor components aren't MVC results, so the admin isn't affected. With
  `UseVisualEditor` on, our filter replaces it and writes no BOM.
- The Blazor admin's pipeline branch skips `/{prefix}/api` (`AdminBlazorRoutes.ApiRoute`), where
  `Cofoundry.Web.Admin.Api` lives by default once the Blazor admin takes the `admin` directory.

## Status and next steps

- **Ported:** shell/nav, sign in/out, forgot/reset/forced change password, My Account, first-run setup, error
  boundary and session check, shared UI components, Dashboard (`/admin-next` and `/admin-next/dashboard`),
  Pages (list, create, details, change URL, duplicate), Images and Documents (list, filter, upload, details,
  replace file, delete), Site Settings (edit, clear cache), image picker (OpenGraph image on pages), Roles
  (permissions editor), Users (per user area: list, create, edit, reset password, delete), Directories (list,
  create, details, change URL, delete), page/directory access rules (`Components/AccessRules`) and Page
  Templates (read-only list and details), Custom Entities phases 1 to 4 (list, create, details, versions,
  Change Ordering, with the dynamic data-model form in `Components/DataModels`: every field type, including
  `[CustomEntityMultiTypeCollection]` (`DataModelCustomEntityMultiTypeCollectionField`, mostly used by page
  blocks; the Field Tests entity has one too). That includes dates/times in the browser's
  time zone (`BrowserTimeZone`), option lists (api sources fetched in the browser), image/document/page
  collections and pickers with preset filters, directory selector, the TinyMCE html editor (AngularJS admin's
  4.4.3 build, `wwwroot/lib/tinymce`), an unsupported-field placeholder, and the
  `IDataModelFieldComponentRegistration` extension point), and the plugins' Blazor packages:
  `Cofoundry.Plugins.ErrorLogging.Admin.Blazor` (error log list and details) and
  `Cofoundry.Plugins.YouTube.Admin.Blazor` (the `[YouTube]` field and picker) and
  `Cofoundry.Plugins.Vimeo.Admin.Blazor` (the `[Vimeo]` field and picker), and the visual editor
  (`Infrastructure/VisualEditor`, `Components/VisualEditor`, `wwwroot/js/visual-editor*.js`; see below).
- **Builds and runs.** Smoke-tested in `samples/src/Dev/Dev.Sandbox` with the `run-blazor-admin` skill (`smoke`
  for Pages, `assets-step.js` for Images/Documents, `settings-step.js` for Site Settings, `image-picker-step.js` for the picker, `roles-users-step.js` for Roles
  and Users, `access-rules-step.js` for Directories and access rules, `page-templates-step.js` for Page Templates, `custom-entities-step.js`, `custom-entity-pickers-step.js`, `nested-data-models-step.js` and `field-types-step.js` (run with `BROWSER_TZ`, uses the dev-only Field Tests entity `SIMFLD`) for Custom Entities, `plugins-step.js`
  for the Error Logging, YouTube and Vimeo plugins; `visual-editor-step.js` for the visual editor (creates and
  deletes its own page, blog post and author); its Vimeo steps need internet access; `auth-step.js` for
  forgot/reset/change password and the setup redirect; `setup-test.js` (standalone, needs a sandbox on an
  empty database, see SKILL.md) for first-run setup; `directory-name-test.js` (standalone, a second sandbox
  with `Cofoundry__AdminBlazor__DirectoryName=cms`, see SKILL.md) for a non-default prefix; `account-step.js` for My Account and editing yourself
  in Users; `error-boundary-step.js` (uses the sandbox's dev-only `/admin-next/test/error` page) for the error
  boundary; `session-step.js` for the session check; `image-upload-size-step.js` for picker upload size rules;
  `upload-remove-step.js` (uses the dev-only `/admin-next/test/upload-fields` page) for optional upload fields;
  `admin-api-step.js` for `Cofoundry.Web.Admin.Api` (compares its GETs with the original's, writes through it);
  `smoke` also deletes the page it duplicates). The steps that open a page's details need at least one page.
- **Dev.Sandbox has a "Member" user area** (`Cofoundry/UserAreas/MemberUserArea.cs`, code `MEM`) so access rules
  and a non-admin users list can be exercised. Access Control is hidden on sites with only the admin area.
- **Server-side validation:** fields join their `AdminForm` (`AdminFormFields`, from `FormFieldBase`), and
  `FormStatus` files each command error under the field showing its property, as the AngularJS
  `validationErrorService` did. Only unmatched errors are listed. Tested by `server-validation-step.js`.
  The domain flattens only one level of `[ValidateObject]`, so the executor re-validates with
  `CommandValidationErrors`, which files a nested data model item's own messages under its collection field.
- **Search-as-you-type** (`FormFieldFilteredDropdown`, also the access rule role picker) must not render the typed
  text back as the input's value: a late render overwrites keystrokes typed since. It only renders a value to clear.
- **Extension points:** `IAdminBlazorModuleRegistration` adds module pages from other assemblies (router and
  endpoints pick up their assembly; the nav links to them). The sandbox's Products module
  (`Dev.Sandbox/Cofoundry/Admin/Blazor`) is the example, tested by `extension-module-step.js`. A plugin package
  that can be installed without its AngularJS one describes its module in `AdminBlazorModule.Module`, which is
  only used when no other registration has that code (Cofoundry doesn't merge duplicate module codes).
- **Plugins:** Blazor admin parts are separate `*.Admin.Blazor` projects next to the AngularJS ones (both in the
  solution's plugin folders). Dev.Sandbox references both versions of Error Logging, YouTube and Vimeo, and has a
  `test/error` route that throws so there is an error to log. The plugin installs its own table
  (`CofoundryPlugin.Error`) on startup.
- **Without the AngularJS admin:** the sandbox's `-p:BlazorAdminOnly=true` build leaves out `Cofoundry.Web.Admin`
  and the plugins' AngularJS packages (see "Blazor admin only" in SKILL.md). Every browser step passes on it,
  and `directory-name-test.js` passes with the admin at `/admin` and the API at `/admin/api`. The built-in
  modules' nav entries came from `Cofoundry.Web.Admin`'s registrations, so `BuiltInAdminModules` describes them
  too, used only for codes no other registration has. Keep the two in step. Steps must work on both builds:
  read data through `/admin-next/api`, never `/admin/api`, and use `BASE_URL`, not a hard-coded port.
- **Visual editor:** the site's pages get the Blazor editor when `Cofoundry:AdminBlazor:UseVisualEditor` is on
  (Dev.Sandbox sets it) or `Cofoundry.Web.Admin` isn't installed. `VisualEditorInstallation` then swaps the
  AngularJS `VisualEditorContentFilter` (matched by type name) for ours, which injects `visual-editor.css/js`, the
  page data as json (`#cofoundry-sv__data`, not `Cofoundry.PageResponseData`) and the toolbar
  (`VisualEditorToolbar`, rendered with `HtmlRenderer`), as UTF-8 without a BOM. The page script posts actions to
  an iframe showing the Blazor page `/admin-next/visual-editor/frame`, whose dialogs run the block/publish
  commands and then reload the page. `AdminBlazorVisualEditorStateService` replaces the AngularJS
  `?mode=`/`?version=` parsing. Its icons are Font Awesome 4: `VisualEditorFontAwesome` (opt-in, on in Dev.Sandbox)
  links the admin's copy (`css/font-awesome.css`) or `VisualEditorFontAwesomeUrl` for sites without their own.
- **Next:** the remaining known gaps in the README. Every plugin with an AngularJS admin part now has a Blazor
  package.
- **Unit tests:** `test/Cofoundry.Web.Admin.Blazor.Tests` (xunit v3, like the repo's other test projects, plus
  bUnit; Razor SDK, so tests can be `.razor` files) covers the shared form components with bUnit (`Components/Ui`
  and `Components/DataModels` tests: `FormFieldText`, `FormSection`/`FieldHelp`, `FormStatus`, `Pager`, the
  upload fields' Required/Remove, `DataModelFieldSet`) and pure helpers: routes, `StringUtilities`, `AdminFormFields`, `FieldValidationMessage.UseTitle`,
  `ImageAssetFilter`, `CommandValidationErrors`, `AdminBlazorMailTemplateBuilder` (with and without `UseForEmailLinks`),
  `HtmlDocumentScriptInjector`, `VisualEditorInstallation` and the plugins' `ParseVideoId`. Run `dotnet test test/Cofoundry.Web.Admin.Blazor.Tests`. Pages are covered by the skill's
  browser steps. A test that binds with `@bind-Value` has no parent component to re-render, so call
  `cut.Render()` after an event that sets the value outside an `Input*` component (see `UploadFieldTests`).

## Git

- Work on the `blazor-admin-port` branch. `origin` is the user's fork (`Paul-Codes-Stuff/cofoundry-blazor`);
  `upstream` is `cofoundry-cms/cofoundry` and is read-only.
