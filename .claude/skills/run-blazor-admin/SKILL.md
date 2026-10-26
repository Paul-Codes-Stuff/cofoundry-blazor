---
name: run-blazor-admin
description: Run, start, smoke-test or screenshot the Blazor admin (/admin-next) in the Dev.Sandbox site, and drive it in headless Chrome. Use to verify a change to Cofoundry.Web.Admin.Blazor actually works in the browser (sign in, forgot/reset/change password; pages list, create, edit, publish, change URL, duplicate; image/document upload, edit, filter, delete; site settings; image picker; roles and permissions; users create, deactivate, reset password, delete; directories; page and directory access rules; page templates; custom entities, dynamic data-model fields, custom entity pickers, versions and ordering, nested data models; every data-model field type: dates and times in the browser's time zone, option lists, image/document/page/directory pickers, the html editor; module pages from other assemblies; the Error Logging, YouTube and Vimeo plugins).
---

# Run the Blazor admin

The Blazor admin (`src/Cofoundry.Web.Admin.Blazor`) is a class library, so it runs inside the
`samples/src/Dev/Dev.Sandbox` site at `http://localhost:5006/admin-next`. You drive it with
`driver.js`, which runs Playwright against the installed Google Chrome in headless mode. All paths
here are relative to the repo root. Verified on Windows 11 with Git Bash, .NET SDK 10.0.402,
Node 14.18 and a local SQL Server.

## Prerequisites

- **SQL Server** that the sandbox can reach. The connection string is `Cofoundry:Database:ConnectionString`
  in `samples/src/Dev/Dev.Sandbox/appsettings.json`. In Development the site also loads a git-ignored
  `appsettings.local.json`, which is the better place for machine-specific overrides.
- **An existing admin user** in that database. Ask the user for the email and password. Don't commit them.
- **Google Chrome** installed (the driver uses `channel: 'chrome'` and doesn't download a browser).
- **playwright-core 1.32.3**, installed outside the repo. 1.32 is the last version that runs on Node 14:

```bash
PW_DIR="$TEMP/blazor-admin-pw"; npm i --prefix "$PW_DIR" playwright-core@1.32.3
```

## Launch the site (background)

```bash
cd samples/src/Dev/Dev.Sandbox && ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5006 > "$TEMP/sandbox.log" 2>&1
```

Run that with `run_in_background`. It builds first, which takes about 30-60s. Then wait for the ready line:

```bash
until grep -qE "Now listening|error CS|build failed" "$TEMP/sandbox.log"; do sleep 1; done; grep -E "Now listening|error CS" "$TEMP/sandbox.log"
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5006/_framework/blazor.web.js   # must be 200
```

Server-side exceptions (circuit crashes, domain validation) only appear in `$TEMP/sandbox.log`.
The browser just says "There was an unhandled exception on the current circuit". To see them:

```bash
grep -A6 -E "fail:|Unhandled|ERROR executing" "$TEMP/sandbox.log" | grep -vE "^\s+at (Microsoft|System)\."
```

## Drive it (agent path)

```bash
export NODE_PATH="$TEMP/blazor-admin-pw/node_modules" ADMIN_USER='<admin email>' ADMIN_PASS='<password>'
node .claude/skills/run-blazor-admin/driver.js smoke                               # Pages module end-to-end
node .claude/skills/run-blazor-admin/driver.js shot admin-next/pages/new create-form   # one screenshot + page text
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/assets-step.js  # Images + Documents
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/settings-step.js  # Site Settings
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/image-picker-step.js  # OpenGraph image picker
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/roles-users-step.js  # Roles + Users
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/access-rules-step.js  # Directories + access rules
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/page-templates-step.js  # Page Templates
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/custom-entities-step.js  # Custom Entities
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/custom-entity-pickers-step.js  # pickers, versions
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/nested-data-models-step.js  # nested models, ordering
BROWSER_TZ=America/New_York node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/field-types-step.js  # every field type
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/extension-module-step.js  # module from another assembly
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/plugins-step.js  # error log, YouTube and Vimeo fields
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/server-validation-step.js  # errors under fields
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/auth-step.js  # forgot/reset/change password
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/account-step.js  # My Account, editing yourself
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/error-boundary-step.js  # unexpected errors
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/session-step.js  # signed out in another tab
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/image-upload-size-step.js  # picker upload size rules
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/upload-remove-step.js  # optional upload fields' Remove
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/admin-api-step.js  # Cofoundry.Web.Admin.Api
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/visual-editor-step.js  # visual editor on the site
STYLES_BASELINE=1 node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/styles-step.js  # before a style change
node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/styles-step.js  # after: compares screenshots
node .claude/skills/run-blazor-admin/driver.js run path/to/step.js                 # your own steps
```

- **`smoke`** prints `[PASS]`/`[FAIL]` for each step: list, create validation, create, edit, change URL,
  publish/unpublish, duplicate, delete (the copy) and list + filter. It exits 1 on any failure, and a failing
  step saves a `fail-<step>.png`. Each run **leaves one real page** with a unique `smoke-<stamp>` slug in the
  database (other steps use it), and the list is oldest first, so the list step pages through it to find it.
- **`server-validation-step.js`** checks a server-side error is shown under its field: it adds a directory
  twice with the same url path, expects the domain's "already exists" message under Url Path (not in the
  form status list), and that editing the field clears it. It deletes the directory.
- **`assets-step.js`** uploads a small PNG, a 4000x2000 PNG (checks it's resized in the browser to 3200x1600)
  and a .txt document, then edits, replaces the file, filters and deletes them. The test files are drawn on a
  canvas in the page, so there are no fixtures. It cleans up after itself when it passes.
- **`settings-step.js`** checks the Help toggle, cancel, required validation, saving (verified through
  `/robots.txt`) and Clear Cache. It appends a marker to robots.txt and then restores the original value.
- **`image-picker-step.js`** picks, changes, uploads-from-picker and removes the OpenGraph image on the first
  `smoke-` page in the list, so run `smoke` once first on a fresh database. It deletes the images it uploads.
- **`roles-users-step.js`** creates a role with Pages permissions, edits them, creates a Cofoundry admin user in
  it, filters, resets the password, deactivates the user, then deletes the user and the role. The user is moved
  to another role before it's deleted, because a role still referenced by a (soft-)deleted user can't be
  deleted. Reset-password and welcome emails go to the sandbox's mail drop folder (`App_Data/Emails`, the
  default `LocalDrop` mode), so nothing is sent. Creating a user takes a few seconds (password hashing).
- **`access-rules-step.js`** creates a directory and a child directory, a Member role and a published page in
  the child, then edits directory and page access rules. It checks that a signed-out request for the page
  returns 200 before the rules and 404 after, using a cookie-less request context: the admin cookie would
  otherwise be sent. It needs the sandbox's `Member` user area (`MemberUserArea.cs`). Without it the admin
  hides Access Control. It deletes the directory (and with it the child and the page) and the role.
- **`page-templates-step.js`** checks the read-only Page Templates list, name filter, details and the page count
  link to the filtered pages list. It creates nothing.
- **`custom-entities-step.js`** covers the sandbox's Authors, Categories, Blog Posts and Examples: the custom
  title term, create/edit/cancel/duplicate/delete with dynamic text, text area and image fields, data-model
  validation (`[Required]` and the schema's required non-nullable image id), [PreviewImage]/[PreviewDescription]
  list columns. Field labels come from MVC metadata, so their casing varies ("Short description:"); the step
  matches labels case-insensitively. It deletes what it creates.
- **`custom-entity-pickers-step.js`** creates authors, categories and an image, then a Blog Post (the only
  sandbox type that doesn't auto-publish) through the Author selector (search as you type, Enter, inline Add)
  and the Categories collection (picker, drag to reorder, remove). It then covers publish, draft + discard, a
  second version, copy V1 to draft and unpublish. Unpublish turns the published version back into a draft, so
  copy-to-draft needs two published versions. Change Ordering is only opened here: Category declares
  `Ordering` but doesn't implement `IOrderableCustomEntityDefinition`, so it isn't orderable. Drag-reorder is driven with Playwright's `dragTo` (HTML5 drag events
  work in headless Chrome). It deletes what it creates.
- **`nested-data-models-step.js`** creates an Example (the only orderable sandbox type) through its nested
  collections: Blocks (one type, `MinItems = 2`) and the multi-type TestCollection (types added from a hover
  menu). It covers the item dialog (validation, add, edit, cancel), the image picker stacked on the item dialog,
  drag-reorder, MinItems validation, saving and editing. Then it duplicates the Example and reorders the two
  with Change Ordering, checking the list order. It deletes what it creates. With stacked dialogs,
  `.modal` matches every open dialog: use `page.locator('.modal').last()` for the top one.
- **`field-types-step.js`** fills every field of the sandbox's dev-only **Field Tests** entity (code `SIMFLD`,
  `Cofoundry/CustomEntities/FieldTests`, one property per field type) and checks what the domain stored through
  the AngularJS admin's api (`/admin/api/custom-entities/{id}`, `data.latestVersion.model`). It covers dates and
  times, option lists (enum, `IListOptionSource` and api sources), the asset pickers' preset filters and upload
  warnings, image/document uploads from the pickers, collections (multi-select, drag, remove), page and
  directory fields, the multi-type custom entity collection (Related: it creates two categories and an author,
  picks both types from the add menu, drags, re-picks one type and removes, and deletes them at the end), the TinyMCE html editor (typing into its iframe, inserting an uploaded image) and the
  unsupported-field placeholder. Run it with `BROWSER_TZ` set to a zone that isn't UTC (the driver passes it to
  the browser as `timezoneId`), or the "Local" date fields don't actually convert. The page fields use existing
  pages, so run `smoke` once first on a fresh database. It deletes the entity and the assets it uploads.
  `c.btn('.modal', ...)` finds the **bottom** dialog's button when dialogs are stacked, and a click on it times
  out under the overlay: scope to `.modal` `.last()` instead.
- **`plugins-step.js`** covers the plugins' Blazor packages. Error Logging: it logs an error through the
  sandbox's `test/error` route, then checks the module is listed once in **both** admins' navs (it fetches the
  AngularJS `/admin` page's html), the list, filter, details and not-found pages. Logged errors stay in the
  database (the plugin can't delete them). YouTube: the `[YouTube]` Video property of Field Tests (`SIMFLD`):
  id and url parsing in the picker, title/description, save, cancel and remove. Vimeo: the `[Vimeo]` property
  on the same entity, with **real** oEmbed lookups (needs internet): video 1 for Vimeo's 404 message, a
  channel url for "The Mountain" (22439234), and the stored duration, upload date and thumbnail. It deletes the
  entity. The Settings sub-nav is hidden until hovered, so its links need `dispatchEvent('click')`, not
  `click()`. A page that embeds the Vimeo player never reaches network idle, so `c.settle()` times out on it:
  wait for an element instead.
- **`auth-step.js`** creates a Cofoundry admin user, then in a signed-out context signs in with the emailed
  temporary password (sent to change password), changes it (validation and a wrong current password first),
  requests a reset from forgot password, opens the emailed link (and a bad token), resets the password and
  signs in with it. It reads the emails from `samples/src/Dev/Dev.Sandbox/App_Data/Emails`, so run it from the
  repo root, and checks their links point at `/admin-next`. It deletes the user (a soft-deleted row remains).
  It also checks `/admin-next/setup` redirects to the dashboard (the sandbox is already set up, so the setup
  form can't be run). `mail.js` has the email reader it shares with `account-step.js`.
- **`account-step.js`** creates an admin user the same way and, signed in as it, opens My Account from the nav
  (an interactive link to a static SSR page), edits the email (validation first), changes the password
  (required, mismatch, wrong current password) and checks each save re-issued the auth cookie. Then it edits
  itself in Users ("Edit" opens the static SSR `.../edit` page): an invalid email first, then a new email,
  checking the cookie is re-issued again. It deletes the user.
- **`error-boundary-step.js`** throws from the sandbox's dev-only `/admin-next/test/error` page
  (`Cofoundry/Admin/Blazor/Shared/ErrorBoundaryTestPage.razor`), on a click and on load, and checks the error
  message, "Try again" and that navigating away recovers. It logs four "Error boundary test" errors in
  `sandbox.log`; they're expected.
- **`session-step.js`** copies the admin's cookies into a new browser context, signs out in a second tab and
  checks the first goes to sign in when focused. The check is throttled, so it waits 15 seconds.
- **`image-upload-size-step.js`** opens the blog post thumbnail's picker (min 460x460), uploads a 100x100 image
  (rejected) and a 500x500 one (accepted), and saves nothing.
- **`upload-remove-step.js`** checks a required upload field (Upload Image) has no "Remove", then uses the
  sandbox's dev-only `/admin-next/test/upload-fields` page, whose optional fields are the only ones with it: Remove
  clears a chosen image (back to the saved one, the first image in the library, so it needs one) and a chosen
  document. It clicks Remove for real, since the button has to sit above the transparent file input. Saves nothing.
- **`styles-step.js`** is for stylesheet changes. With `STYLES_BASELINE=1` it screenshots 18 screens (lists,
  forms, the field types form, a dialog, an open search dropdown, the signed-out login) into
  `styles-baseline`; without, it takes them again and fails any screen with more than 0.2% of pixels changed,
  writing `styles-diff-<screen>.png` with the changes in red. Relative times ("15 hours ago") and new data
  show up as small diffs, and the page templates table has once shifted a few pixels with nothing changed,
  so read the diff images before blaming the CSS. Saves nothing.
- **`admin-api-step.js`** covers `Cofoundry.Web.Admin.Api`, which the sandbox serves at `/admin-next/api`
  (`Cofoundry:AdminApi:BaseUrl` in its appsettings.json) next to the original at `/admin/api`. It checks a
  signed-out request gets a 401, that 30-odd GET endpoints return byte-identical bodies from both (per-item
  paths are skipped when their list is empty), that a write without the antiforgery token is refused (it takes
  a token from an AngularJS admin page, as the AngularJS client does), a validation error, and adds, renames
  and deletes a role through the copy. It leaves nothing behind.
- **`visual-editor-step.js`** covers the visual editor on the site's pages (the sandbox sets
  `Cofoundry:AdminBlazor:UseVisualEditor`). It creates a General page and opens it with `?mode=edit`: the toolbar,
  page data and styles, Font Awesome (the sandbox sets `VisualEditorFontAwesome`: the link, the icons' font and
  that it loads), no BOM, adding blocks through the iframe's dialogs (the empty region's "+", "Add below"),
  cancel, edit, move up and delete, where the block popover and "+" (add below) sit (over a Content Section's
  corner and centred on its bottom edge; above a one line Plain text block and past its outline's bottom right
  corner, still adding below), publish (and that a signed-out visitor gets no editor), a second version, copy
  an old version to draft with and without an existing draft, and unpublish. It checks `/test/test`'s json is left
  alone. Then it creates an author, a blog post (with the first image in the library as its thumbnail, so it
  needs one) and a Custom Entity Details page, and adds, edits and deletes a block in the post's region and
  publishes the post. Then it switches to "Edit template" (`edittype=page`): only the template's own region gets
  an overlay, the dialogs say "Page", and adding, editing and deleting a block there and publishing publish the
  details page. It deletes everything it created. About 5 minutes. The frame is
  `page.frameLocator('#cofoundry-src__iFrame')`, and TinyMCE is a frame inside it.
- **`extension-module-step.js`** covers the module extension point through the sandbox's Products module, a
  Blazor page in Dev.Sandbox (`Cofoundry/Admin/Blazor`): the nav link, direct load (endpoints) and in-app
  navigation (router), the signed-out redirect, validation, its custom selector field, save and cancel. The
  product lives in memory, so restarting the sandbox resets it. It creates nothing in the database.
- **`run`** loads a module of the form `module.exports = async (page, c) => { ... }`. `page` is a
  Playwright page with `baseURL` already set. `c` provides `settle()`, `shot(name)`, `step(name, fn)`,
  `btn(scope, text)`, `field(scope, label)`, `rows()` and `status()`. See `driver.js`.
- Screenshots and the saved sign-in cookie (`auth-state.json`) go to `$TEMP/blazor-admin-run`
  (override with `OUT_DIR`). Delete `auth-state.json` to force a fresh sign-in.
- Browser console errors, page errors and HTTP 4xx/5xx responses are printed as they happen.
  **Read the screenshots.** A page can render with missing styling and still pass a DOM check.

## First-run setup (empty database)

`setup-test.js` runs without `driver.js`, which signs in first. It needs a second sandbox on an **empty**
database, so create one, point the sandbox at it with an environment variable (no config file changes), run
the test, then drop the database. Setup only runs once per database, so each run needs a fresh one.

```bash
sqlcmd -S <server> -E -C -Q "CREATE DATABASE CofoundryBlazorSetupTest"
# run_in_background:
cd samples/src/Dev/Dev.Sandbox && ASPNETCORE_ENVIRONMENT=Development Cofoundry__Database__ConnectionString="Server=<server>;Database=CofoundryBlazorSetupTest;Integrated Security=True;MultipleActiveResultSets=True;TrustServerCertificate=True;" dotnet run --no-launch-profile --urls http://localhost:5007 > "$TEMP/sandbox-setup.log" 2>&1
# once "Now listening" is in the log:
NODE_PATH="$TEMP/blazor-admin-pw/node_modules" node .claude/skills/run-blazor-admin/setup-test.js http://localhost:5007
# stop Dev.Sandbox, then:
sqlcmd -S <server> -E -C -Q "DROP DATABASE CofoundryBlazorSetupTest"
```

Cofoundry installs its tables on startup. The test checks sign in sends you to setup, validation (required
fields, mismatched and too-short passwords), completing setup, signing in as the new admin (a throwaway
`setup-<stamp>@example.com`), and that setup then redirects to the dashboard.

## Directory name (non-default prefix)

The steps above assume `/admin-next`. `directory-name-test.js` checks the admin under another
`Cofoundry:AdminBlazor:DirectoryName`, against a second sandbox on the same database and build (stop nothing;
`--no-build` avoids fighting the first sandbox over the dlls):

```bash
# run_in_background:
cd samples/src/Dev/Dev.Sandbox && ASPNETCORE_ENVIRONMENT=Development Cofoundry__AdminBlazor__DirectoryName=cms dotnet run --no-build --no-launch-profile --urls http://localhost:5008 > "$TEMP/sandbox-cms.log" 2>&1
# once "Now listening" is in the log:
NODE_PATH="$TEMP/blazor-admin-pw/node_modules" node .claude/skills/run-blazor-admin/directory-name-test.js http://localhost:5008 cms
```

It checks the signed-out redirect to `/cms/auth/login` (with styles applied), sign in, that no nav link
points at `/admin-next`, in-app navigation and back, a dialog, direct loads of the root and a static SSR page,
the session check, that `/admin-next` no longer serves the admin, and sign out. It opens the first page's
details, so it needs at least one page (the "nav links" and "dialog" steps fail on an empty pages list). Stop that sandbox only, e.g. in
PowerShell: `Get-CimInstance Win32_Process -Filter "Name='Dev.Sandbox.exe'" | ? CommandLine -match 5008 | % { Stop-Process -Id $_.ProcessId }`.

## Blazor admin only (no AngularJS admin)

`dotnet build -p:BlazorAdminOnly=true` builds Dev.Sandbox without `Cofoundry.Web.Admin`, the plugins' AngularJS
packages and the sandbox's AngularJS Products module (`Cofoundry/Admin/Modules`), into `bin/BlazorAdminOnly` and
`obj/BlazorAdminOnly`, so it runs beside the normal build on the same database. Check the output has no
`Cofoundry.Web.Admin.dll`. The two builds share `obj/project.assets.json`, so build them one after the other (each
restores itself), and stop the sandboxes first as usual.

```bash
cd samples/src/Dev/Dev.Sandbox && dotnet build -p:BlazorAdminOnly=true
# run_in_background; the admin stays at /admin-next, so every step runs unchanged:
cd samples/src/Dev/Dev.Sandbox && ASPNETCORE_ENVIRONMENT=Development dotnet run --no-build -p:BlazorAdminOnly=true --no-launch-profile --urls http://localhost:5008 > "$TEMP/sandbox-blazoronly.log" 2>&1
# then any step, with BASE_URL (and an OUT_DIR, so the sign-in cookie and screenshots don't mix):
BASE_URL=http://localhost:5008 OUT_DIR="$TEMP/blazor-admin-run-only" node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/account-step.js
```

Every step and `smoke` pass on it. Steps that compare with the AngularJS admin skip that part when `/admin`
404s: `admin-api-step.js` checks the copy's GETs only, `plugins-step.js` skips the AngularJS nav count. Steps
read stored values through `/admin-next/api` (the copy), never `/admin/api`, and must use `BASE_URL` (not a
hard-coded port) for any request context of their own. To check the admin where it would live, at `/admin` with
the API at `/admin/api`, restart that sandbox with the directories set and run `directory-name-test.js` with
the API path:

```bash
# run_in_background:
cd samples/src/Dev/Dev.Sandbox && ASPNETCORE_ENVIRONMENT=Development Cofoundry__AdminBlazor__DirectoryName=admin Cofoundry__AdminApi__BaseUrl=admin/api Cofoundry__Admin__DirectoryName=admin dotnet run --no-build -p:BlazorAdminOnly=true --no-launch-profile --urls http://localhost:5008 > "$TEMP/sandbox-blazoronly.log" 2>&1
OUT_DIR="$TEMP/blazor-admin-run-only" node .claude/skills/run-blazor-admin/directory-name-test.js http://localhost:5008 admin admin/api
```

Stop it alone with the PowerShell line in "Directory name".

## NuGet compatibility

The new projects (the Blazor admin, `Cofoundry.Web.Admin.Api`, the plugins' `*.Admin.Blazor`) must work with the
published Cofoundry packages, so the branch leaves Core, Domain, Web, `eng/Base.props` and the plugins' other
projects as released (see CLAUDE.md). To check, with internet access (no sandbox needed, about 2 minutes):

```bash
node .claude/skills/run-blazor-admin/nuget-compat-check.js          # latest stable Cofoundry.Web
node .claude/skills/run-blazor-admin/nuget-compat-check.js 0.13.2   # a given release
```

It reads the commit the release was built from (its nuspec's `repository commit`) and fails if those projects
differ from it here, then copies the new projects' tracked files to a temp folder, swaps every reference to
another project for that project's package at the same version, and builds each. `KEEP=1` keeps the copy and
prints its path. A release whose commit isn't in this clone skips the source half: fetch
`cofoundry-cms/cofoundry` first. Checked: 0.13.2 passes both; 0.13.1 fails the source half (its csproj package
versions differ) and still builds.

## Admin API base url collision

`Cofoundry.Web.Admin.Api` refuses to start at the AngularJS admin's own API path while `Cofoundry.Web.Admin` is
installed. To check, start a second sandbox with the colliding setting; it should exit at once with "already
serves the admin API at "admin/api"" in the log:

```bash
cd samples/src/Dev/Dev.Sandbox && Cofoundry__AdminApi__BaseUrl=admin/api ASPNETCORE_ENVIRONMENT=Development timeout 90 dotnet run --no-build --no-launch-profile --urls http://localhost:5008 > "$TEMP/sandbox2.log" 2>&1
grep "serves the admin API" "$TEMP/sandbox2.log"
```

## Unit tests

```bash
dotnet test test/Cofoundry.Web.Admin.Blazor.Tests
```

Pure helpers only (no database or browser needed); everything else is covered by the steps above.

## Stop

```powershell
Get-Process Dev.Sandbox | Stop-Process -Confirm:$false
```

## Run (human path)

Launch it the same way, open http://localhost:5006/admin-next (the dashboard), and sign in. The AngularJS admin
is at `/<Cofoundry:Admin:DirectoryName>` (`/admin` or `/sandbox-admin`, depending on appsettings), so you
can compare the two side by side.

## Gotchas

- **Visual editor overlays redraw 500ms after a window resize**, and `c.shot` (a full page screenshot) resizes
  the window. Wait after a shot before hovering a block, and move the mouse off it first: after a reload the
  pointer may already be inside, which fires no `mouseenter`. Move up/down, add above/below and delete are in the
  "..." menu, which opens on hover.
- **Hover a block at its bottom left with `page.mouse.move`**, after scrolling it 200px below the top. The
  site's fixed navbar covers blocks near the top, the block's name label covers its top left and its "+" (add
  below) the middle of a short block, so `locator.hover()` keeps retrying "element intercepts pointer events"
  and times out. The popover sits over a block's top left corner, or just above a block less than twice its
  height (beside the name label); `popoverPlacement` in the step checks both. A short block's "+" moves past the
  bottom right corner of its outline (`addButtonPlacement`).
- **Git Bash rewrites arguments that start with `/`** into Windows paths. `/admin-next/pages` becomes
  `C:/Program Files/Git/admin-next/pages`. Pass driver paths without the leading slash. For curl, use full
  URLs, or `MSYS_NO_PATHCONV=1`.
- **Wait after navigation.** There's no prerendering, so the page body is empty until the SignalR circuit
  connects. `networkidle` fires too early. The driver's `settle()` waits an extra ~1.2s.
- **Text inputs update on `change`, not on each keystroke.** After `fill()`, call `.blur()` before reading
  anything that depends on the value, such as the URL path that's auto-generated from the title.
- **`getByLabel('Title:')` matches "OpenGraph Title:" too.** Always pass `{ exact: true }`, or use `c.field`.
- **Two kinds of button.** Primary submits are `<input type=submit value="...">` (Save Draft, Apply,
  Create duplicate). Everything else is a `<button>`. `c.btn` handles both. Confirm dialogs use
  "Yes, publish it"-style labels, so click `.modal .main-cta` instead of matching the text.
- **Dialogs render outside `.content`**, through a `SectionOutlet` in `AdminLayout`. If you ever see a
  dialog's left edge under the nav and clicks time out, that outlet has been lost.
- **A favicon 404 appears on every page.** The sandbox has no `/favicon.ico`. Ignore it. The AngularJS
  admin also logs a 404 for `ajax-loader.gif`, which is a bug in its own CSS.
- **Uploading files:** call `setInputFiles({ name, mimeType, buffer })` on `.form-field-image-upload
  input[type=file]` (both upload fields use that class). The input is a transparent overlay on the drop area.
  Wait ~1.5s afterwards: the file is streamed to the server before the field updates.
- **Labels are upper-cased by CSS**, so `innerText` reads `PATH:`, not `Path:`. Match case-insensitively.
- **Document file names come from the title**, so `smoke-doc.txt` is listed as `Smoke-doc.txt`.
- **Sign-in/out pages are static SSR, not interactive.** `/admin-next/auth/login` is a real form POST,
  so wait with `waitForNavigation`, not `settle()`. To test them signed out, open a fresh context with
  `page.context().browser().newContext({ baseURL })`, because the driver's own context is already signed in.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Blank grey page, `[http 404] .../_framework/blazor.web.js` | The host is missing `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>`. The SDK only adds the Blazor script when the host itself has `.razor` files. |
| Page body empty, log shows `Authentication schemes cannot be specified for components` | Something put `AuthenticationSchemes` on a component's `[Authorize]`. Use `[AdminBlazorAuthorize]`, which carries the scheme in its policy. |
| `Playwright requires Node.js 18 or higher` | You installed a newer playwright-core. Pin it to `1.32.3`. |
| `strict mode violation: getByLabel(...) resolved to 2 elements` | Add `{ exact: true }`. |
| Input shows literal text like `Value` or `_command.Title` | A `string` component parameter was given a bare name. Write `Value="@x"`. |
| `error CS...` in `sandbox.log`, then the background task exits 1 | Build error. Run `dotnet build src/Cofoundry.Web.Admin.Blazor` to see it clearly. |
