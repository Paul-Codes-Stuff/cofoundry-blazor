# Cofoundry.Web.Admin.Blazor

A Blazor (Interactive Server) port of the AngularJS admin in `Cofoundry.Web.Admin`. It is designed to be
installed **alongside** the AngularJS admin while modules are migrated one at a time.

> **Status: first slice.** Builds cleanly. The Dashboard, Pages (list, filter, create, details/edit, publish,
> unpublish, change URL, duplicate, access rules), Directories, Page Templates, Images, Documents, Site Settings, Roles, Users
> and Custom Entities (phases 1 to 4: every data-model field type except page blocks' multi-type collection)
> modules have been smoke-tested in `Dev.Sandbox`.

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
  there; `/admin/...` still uses the MVC login. Forgot password, reset password and forced password change
  are at `/admin-next/auth/forgot-password`, `reset-password` and `change-password`. First-run setup is at
  `/admin-next/setup`, where the auth pages send you until the site is set up.
* The nav lists every permitted module. Modules with a Blazor page (see "Extending the admin") link to it;
  the others, including custom modules not yet ported, link to their AngularJS admin URL.

### Changing the prefix

Razor `@page` directives only accept string literals, so the prefix is repeated in each page component.
Change `AdminBlazorRoutes.Prefix` **and** every `@page "/admin-next/..."` directive.

## How it differs from the AngularJS admin

| AngularJS admin | Blazor admin |
| --- | --- |
| Browser calls `/admin/api/*` controllers | Components call the domain layer in-process via `IAdminBlazorExecutor` |
| `HttpContext` identifies the user | A per-operation DI scope is seeded with the circuit's signed-in user (`CircuitAwareUserSessionService`) |
| `cms-*` directives | Razor components in `Components/Ui`, emitting the same markup so the AngularJS admin's styles apply (their SCSS is now in `Styles`) |
| Self-loading selector directives | `AdminLookups` loads dropdown data up front |
| `$routeProvider` per module | `@page` routes |
| `validationErrorService` sends a server error to the field registered for its property | Fields join their `AdminForm` (`AdminFormFields`); `FormStatus` files each command error under the field showing that property and lists only the rest. Data model fields join under the data model property's name |
| Client-side messages per validation attribute ("This field is required") | The command's DataAnnotations messages, with the field's label in place of the property name (`FieldValidationMessage`): "The Application Name field is required." |
| A server error opens an error dialog | `AdminLayout` wraps each page in an `ErrorBoundary`: the page shows the error (with the exception where the developer exception page is allowed) and the nav keeps working. Errors outside a page show Blazor's `#blazor-error-ui` bar (`App.razor`) |
| A 401 from the api sends you to sign in | A circuit keeps its user, so `js/session-check.js` asks `/admin-next/auth/session` whenever the tab is shown again and reloads the page if you've been signed out (in another tab, expired, or your password changed elsewhere), which sends you to sign in |
| Every request can re-issue the auth cookie | A circuit can't set cookies. Pages where you change your own email or password (My Account, and editing yourself in Users) are static SSR, so the domain can re-issue the cookie with the new security stamp. `CircuitAwareUserSessionService` skips the refresh inside a circuit instead of throwing |

### Styles

`Styles/admin.scss` compiles to `wwwroot/css/admin.css`, which `App.razor` links. The compiled file is committed,
so building the project doesn't need Node. After changing the SCSS, compile it with Dart Sass (pinned to 1.77.8,
which still runs on Node 14) and commit both:

```bash
cd src/Cofoundry.Web.Admin.Blazor/Styles
npm install
npm run build      # or: npm run watch
```

The SCSS started as a copy of the AngularJS admin's `Shared/Sass` and its `UIComponents` partials (same folder
names, under `Styles/Components`), so the two admins look the same, and the copy now belongs to this project.
The AngularJS build used Ruby Compass mixins, which Dart Sass can't compile: `Base/_reset.scss` holds the reset
as plain CSS and `Libs/_compass.scss` the few mixins still used, without vendor prefixes or IE filters. The
third-party CSS (Font Awesome 4.3, TinyMCE's skin, ui-select and selectize) is in `Styles/ThirdParty` and
compiled in; only their fonts and images stay in `wwwroot/css`. `Blazor/_blazor.scss` is for markup only the
Blazor admin has. The switch was checked with `styles-step.js` in the `run-blazor-admin` skill, which compares
screenshots of the admin with a baseline taken before a change.

## Ported so far

* Shell: layout, permitted-module nav, auth
* Sign in / sign out (static SSR pages in `Components/Auth`, with the same setup, return-URL and
  password-change redirects as the MVC controller). Unlike the MVC login, "Remember me" is honoured:
  unticked gives a browser-session cookie, ticked a persistent one
* Forgot password, reset password (from the emailed link) and forced password change (after signing in with a
  temporary password), also static SSR and sharing `AuthPageBase`. The domain builds the admin emails' links
  from the AngularJS admin's directory, so `AdminBlazorMailTemplateBuilder` (an
  `IUserMailTemplateBuilder<CofoundryAdminUserArea>`, picked up by Cofoundry's scan) points the reset link at
  `/admin-next/auth/reset-password` and sign-in links at `/admin-next`. That applies to emails sent from
  either admin, so the AngularJS forgot password page also emails a Blazor link. A site with its own builder
  for the admin user area replaces this one and keeps the AngularJS links
* My Account (`Components/Account`, static SSR): your details (display name, email, audit data) and change
  password, at `/admin-next/account`. "Edit" links to `?edit=true` and the form posts back, then redirects
* First-run setup (`Components/Setup`, static SSR) at `/admin-next/setup`: application name and the first admin
  user, then a link to sign in. Once set up it redirects to the dashboard
* Shared UI: page layout, buttons, form + fields (text, number, text area, checkbox, dropdown, tags, read-only,
  container, image upload, document upload, image anchor location, date-time-local, decimal), table (incl. image thumbnail, tag list,
  created/updated audit cells), pager, status/warning messages, time-ago, audit data, modal + confirm dialog
* Dashboard: at `/admin-next/dashboard` and the `/admin-next` root. It shows the site's
  `/Cofoundry/Admin/Dashboard/Dashboard.html` if one exists, otherwise the application name and a welcome message
* Pages: list (filter, paging, publish), create, details (edit incl. OpenGraph image, publish/unpublish, versions, discard draft,
  copy to draft, delete), change URL, duplicate, access control. The list takes `?pageDirectoryId=` or
  `?pageTemplateId=` to pre-filter
* Page Templates: list (filter by name, paging), details (file path, page type, model type, regions). Read-only,
  as in the AngularJS admin, since templates are registered from the view files at startup
* Directories: list (the whole tree), create, details (edit name, change URL/parent, delete, child directories)
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
  module to its Blazor list. Your own role and active status are read-only. "Edit" on your own user opens
  `UserEditSelfPage` (`.../edit`), a static SSR form with the same fields (`UserFormFields`), so changing
  your email, username, "Require Password Change" or verification can re-issue your auth cookie
* Access rules for pages and directories (`Components/AccessRules`): `EntityAccessEditorDialog` edits either,
  through an `EntityAccessRuleSource` (`PageAccessRuleSource`, `DirectoryAccessRuleSource`). It shows inherited
  directory rules, adds rules (user area plus optional role), removes them, and sets the sign-in redirect and
  default action. As in the AngularJS admin, "Access Control" only appears once the site has a user area
  besides the Cofoundry admin one, and is read-only without the manage access rules permission

* Custom Entities: one module per definition at `/admin-next/custom-entities/{DefinitionCode}`, with list (filter
  by text/created dates/market, paging, [PreviewImage] and [PreviewDescription] columns), create (Save, Save &
  Publish, Save Draft, Save & Edit Content), details (edit, publish/unpublish, versions, discard draft, copy to
  draft, change URL, duplicate, delete, template regions) and Change Ordering (market step, drag to reorder,
  partial ordering's add/remove). The definition's custom terms, `AutoPublish`, `AutoGenerateUrlSlug`,
  `HasLocale` and `Ordering` drive the UI as in the AngularJS module. Ordering only applies to definitions that
  implement `IOrderableCustomEntityDefinition`. The nav maps every module restricted by
  `CustomEntityAdminModulePermission` to its Blazor list

### Dynamic data-model forms

`DataModelFieldSet` (`Components/DataModels`) is the port of `cms-form-dynamic-field-set`. It takes the domain's
`IDynamicDataModelSchema` and the **real CLR model instance** (not JSON), and renders one component per property,
chosen by the schema's data template name:

* Each property is a `DataModelField`. `Field.Bind<T>()` adapts the property to the type an existing form field
  wants (e.g. an `int` image id to `FormFieldImageAsset`'s `int?`), and `Field.GetAttribute<T>(key)` reads the
  schema's additional attributes (`Maxlength`, `Rows`, `Placeholder`, `Min`/`Max`/`Step`, `PreviewWidth`...).
* The model's own data annotations are validated by the field set, because the form's `DataAnnotationsValidator`
  only checks the top-level command. Each property is validated on its own (`Validator.TryValidateProperty`,
  with the field's title as the display name, so messages read "The Date field is required"), then the
  model's `IValidatableObject` results are added. Messages are filed against the bindings so they show under
  each field. The schema's `IsRequired` (e.g. a non-nullable image id) is also enforced, as the AngularJS
  `required` was, but an empty collection doesn't count as missing: MVC marks every non-nullable collection
  as required, and the AngularJS fields didn't enforce that either.
* Supported: text (String, Char, Url, EmailAddress), MultilineText, Html, Boolean, numbers (including plain
  integers, which the AngularJS admin didn't render), Date, DateLocal, DateTime, DateTimeLocal, Time, Color,
  Dropdown, RadioList, CheckboxList, ImageAsset, ImageAssetCollection, DocumentAsset, DocumentAssetCollection,
  PageSelector, PageCollection, DirectorySelector, CustomEntitySelector (a search-as-you-type
  `FormFieldFilteredDropdown` with an "Add" button that creates one in a dialog), CustomEntityCollection (a
  table filled from `CustomEntityPickerDialog`, which filters, pages and can also create; orderable
  collections reorder by dragging rows), NestedDataModelCollection and NestedDataModelMultiTypeCollection.
  Anything else shows a "can't be edited in this admin yet" placeholder that keeps the value and still shows
  its validation messages.
* Dates and times: `Field.Bind<T>()` converts between the property's type (DateTime, DateTimeOffset, TimeSpan
  or an ISO string) and what the inputs use. The "Local" fields (`[DateLocal]`, `[DateTimeLocal]`) store UTC
  and show the **browser's** time zone, as the AngularJS fields did through the browser's `Date`. The server
  doesn't know that zone, so `BrowserTimeZone` (scoped, `wwwroot/js/time-zone.js`) reads its IANA name once
  per circuit and converts (a time skipped by a daylight saving change uses the zone's standard offset).
* Option lists: an enum or `IListOptionSource` puts its options in the schema. An `IListOptionApiSource` gives
  a relative api path instead, which is fetched **in the browser** (`wwwroot/js/option-source.js`) with the
  user's cookies, as the AngularJS `optionSourceService` did, so existing option apis work unchanged. Values
  are compared as invariant strings, as the AngularJS fields compared them loosely.
* Asset fields: `[Image]`, `[ImageCollection]`, `[Document]` and `[DocumentCollection]` pass their
  restrictions (tags, sizes, file extensions; `DataModelAssetFilters`) to the pickers, which apply them, show
  them disabled in the filter form and keep them on Clear. The upload dialogs opened from a picker warn about
  the restrictions, and the image upload is tagged with the field's tags and rejects an image that doesn't
  meet its sizes ("The image is 100x100 which does not meet the size requirements."), as the AngularJS
  upload field did. The image and document pickers
  have a multi-select mode for the collection fields, which share `IdCollectionFieldBase` with the page
  collection.
* Html: `FormFieldHtml` runs TinyMCE through `wwwroot/js/html-editor.js`. It's the same TinyMCE 4.4.3 build the
  AngularJS admin bundles (copied to `wwwroot/lib/tinymce`, LGPL), and its skin is compiled into `admin.css`.
  The toolbar presets, `Rows`, `CustomToolbar`, `ConfigFilePath` (fetched in the browser) and `ConfigSource`
  options work as before. The "image" toolbar button opens `ImageAssetEditorDialog`. The AngularJS field also
  asked for the `imagetools` plugin, which its build doesn't include (a 404), so it's left out.
* Nested data models: the collection fields show a table of the items' [PreviewTitle], [PreviewDescription]
  and [PreviewImage] values (`NestedDataModelPreview`), and add or edit one item at a time in
  `EditNestedDataModelDialog`, which is a `DataModelFieldSet` of the nested schema. The dialog edits a shallow
  copy, so Cancel discards the changes (the AngularJS dialog edited the item in place, so they survived a
  cancel). That's safe because the collection fields never change a list in place, they always set a new one.
  The multi-type field adds from a hover menu of its types (`Ui/Menu`, the `cms-menu` port), and new items start
  from the schema's `DefaultValue`, so no CLR type lookup is needed.
* **Extension point:** implement `IDataModelFieldComponentRegistration` (discovered automatically) to map
  template names to components deriving from `DataModelFieldComponentBase`. Registrations are applied after the
  built-in `DefaultDataModelFieldComponentRegistration`, so they can replace a built-in field. This is the
  Blazor replacement for plugins adding `cms-form-field-*` directives (e.g. the YouTube plugin).
* Field ids: `FormFieldBase` builds the input id from an optional `Id` parameter (the data model fields pass the
  property name, since they all bind to a binding's `Value`) and a cascaded `FieldIdPrefix`. `ModalDialog`
  cascades a unique prefix, so a dialog's `Title` field doesn't clash with the page's.

### Dialogs

`ModalDialog` renders through a `SectionOutlet` in `AdminLayout`, outside `.content`. Each open dialog takes its
own level from the `ModalDialogStack` that the layout cascades, and the layout has one outlet per level, so a
dialog opened from inside another (the image picker in a nested item dialog) stacks on top of it
(`is-child-modal`), as the AngularJS modal service did. With a single outlet only the newest dialog would
render, and the older dialog's content, including the field that opened the new dialog, would be disposed.

### File uploads

Uploads go through `InputFile` over the SignalR circuit and are buffered in memory on the server
(`BufferedFileSource`) before the domain command runs, because the domain may read a file more than once. The
limit is 30MB (`BufferedFileSource.MaxFileSize`), matching the request size limit the AngularJS admin's api had.
As in the AngularJS admin, images larger than `ImageAssetsSettings.MaxUploadWidth`/`MaxUploadHeight` are resized
to a JPEG in the browser first (`wwwroot/js/image-upload.js`), and the title defaults to the file name.

## Extending the admin

The Blazor equivalents of the AngularJS extension model. Everything is discovered through Cofoundry's usual
assembly scanning, so an extension only needs to be referenced by the site.

* **Modules (pages):** implement `IAdminBlazorModuleRegistration` (`Infrastructure/AdminBlazorModules.cs`),
  returning `new AdminBlazorModule("<AdminModuleCode>", typeof(MyModulePage))`. The nav then links that module
  to the page's `@page` route, which must be under `/admin-next` and have no parameters (pass the url when the
  page has more than one). The page's assembly is added to the router and to the endpoints, so every routable
  component in it works, by direct request or in-app navigation. Later registrations replace earlier ones, so
  a site can also swap a built-in module's page. The built-in modules are registered the same way
  (`DefaultAdminBlazorModuleRegistration`).
  * The module itself (title, menu, ordering, permission) still comes from the domain's
    `IAdminModuleRegistration`. A module ported from AngularJS keeps its `IStandardAngularModuleRegistration`
    and only adds the Blazor registration. A Blazor-only module implements `IAdminModuleRegistration` with its
    `/admin-next/...` url.
  * A package that may be installed with or without its AngularJS counterpart (a plugin) sets
    `AdminBlazorModule.Module` instead. `AdminBlazorModuleDefinitionRegistration` adds that module to Cofoundry's
    list (so both admins' navs show it, linking to the Blazor page) only when no other `IAdminModuleRegistration`
    has its code. Cofoundry doesn't merge modules with the same code, so this is what stops it being listed
    twice when both packages are installed.
  * Pages need `[AdminBlazorAuthorize]` (signed-in admin) and should check the module's own permission with
    `IPermissionValidationService`, as the built-in pages do. They get `AdminLayout` from the router.
* **Form fields:** components in `Components/Ui` (`FormFieldText`, `FormFieldDropdown`, ...) are public and can
  be used or wrapped, like the sandbox's `FormFieldTestSelector`. For a data-model field type (an attribute's
  `TemplateHint`/`UIHint`, e.g. the YouTube plugin's "Youtube"), implement `IDataModelFieldComponentRegistration`
  (see "Dynamic data-model forms").
* **Services:** inject `IAdminBlazorExecutor` for domain queries and commands (it runs them as the signed-in
  user), `AdminLookups` for shared dropdown data, and use the permission records in `AdminPermissions.cs`.
  These replace the AngularJS shared services (`shared.serviceBase`, `permissionValidationService`, ...).
* **Static files:** a Razor class library's `wwwroot` is served at `_content/<AssemblyName>/`.
* **Razor namespaces:** a project whose folders include `Cofoundry` (e.g. a site's `Cofoundry/Admin/...`) gets
  component namespaces like `MySite.Cofoundry.Admin`, which hide the real `Cofoundry` namespaces. Use
  `@using global::Cofoundry...` in its `_Imports.razor` and set `@namespace` in each component.

The sandbox's **Products** module is the worked example: `samples/src/Dev/Dev.Sandbox/Cofoundry/Admin/Blazor`.

### Plugins

The plugins' Blazor admin parts are separate packages next to their AngularJS ones (`*.Admin.Blazor` under
`src/Cofoundry.Plugins`), so a site installs whichever admin it runs, or both during the migration. Dev.Sandbox
references both versions of each.

* **Error Logging** (`Cofoundry.Plugins.ErrorLogging.Admin.Blazor`): the error log module (`COFERR`), a list
  with a text filter and a read-only details page at `/admin-next/errors`. It describes its module itself
  (`AdminBlazorModule.Module`), so it works without the AngularJS package. Compared with the AngularJS pages,
  the third details section is titled "Other" (the AngularJS template repeated "Stack Trace"), the date is
  formatted (UTC), and a long stack trace scrolls instead of spilling out of its section. The plugin's own
  query maps `Session` from the query string (a domain bug, left as it is).
* **YouTube** (`Cofoundry.Plugins.YouTube.Admin.Blazor`): the `[YouTube]` field ("Youtube" template hint) with
  its picker dialog. The video is looked up with the YouTube Data API **on the server** (`YouTubeVideoInfoService`)
  rather than in the browser, so the API key (`YouTubeSettings.ApiKey`) is no longer sent to the
  browser, and the `youtube-settings` admin api isn't needed. Without a key only the id is kept, as before. The
  field shows the video's title and description under the preview, which the AngularJS field meant to do but
  didn't (it checked a misspelt property). The AngularJS field's id-only mode (`cms-model-type="id"`) isn't
  ported: data model forms never used it.
* **Vimeo** (`Cofoundry.Plugins.Vimeo.Admin.Blazor`): the `[Vimeo]` field ("Vimeo" template hint), the same
  shape as YouTube. Videos are looked up with Vimeo's oEmbed api (no key) from the server
  (`VimeoVideoInfoService`), sending the admin's url as the referrer as the browser did, so videos restricted
  to the site's domain still resolve. The 403/404/other error messages are the AngularJS ones, and html is
  stripped from the description as before. Same title/description display fix and missing id-only mode as
  YouTube.

## Known gaps

* The YouTube field's lookup with an API key hasn't been tested (the sandbox has no key), and the preview shows
  YouTube's "Error 153" in headless Chrome. The AngularJS field embeds the same url
* `[CustomEntityMultiTypeCollection]` (only used by page blocks) isn't ported; it comes with page block editing
* The page picker (page collection field) is multi-select only, since the single page field is a
  search-as-you-type dropdown, as in the AngularJS admin
* Upload fields have no "Remove" button. The AngularJS field only shows it when the field is optional, and every
  image and document upload form (in both admins) requires a file
* **To revisit:** the close icon (top right) on modal dialogs (`ModalDialog`) was reported as doing nothing. It
  couldn't be reproduced since: in headless Chrome the icon is the topmost element and a real click closes the
  image/document pickers, upload dialogs (stacked on a picker), Duplicate and the access rules editor. If it
  happens again, note which dialog and browser
* Visual editor (stays on the MVC site; links use `?mode=edit`)
* A role can't be deleted while any user references it, including soft-deleted users (a domain rule, the same
  in the AngularJS admin). Move users to another role before deleting them if the role should go too
* Unit tests (`test/Cofoundry.Web.Admin.Blazor.Tests`) cover the pure helpers only (routes, string utilities,
  the server-error field matching, nested item errors, message wording, video id parsing, image size
  restrictions). Components and
  pages are covered by
  the browser steps in `.claude/skills/run-blazor-admin`
