---
name: run-blazor-admin
description: Run, start, smoke-test or screenshot the Blazor admin (/admin-next) in the Dev.Sandbox site, and drive it in headless Chrome. Use to verify a change to Cofoundry.Web.Admin.Blazor actually works in the browser (pages list, create, edit, publish, change URL, duplicate; image/document upload, edit, filter, delete; site settings; image picker; roles and permissions; users create, deactivate, reset password, delete; directories; page and directory access rules; page templates; custom entities, dynamic data-model fields, custom entity pickers, versions and ordering, nested data models).
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
node .claude/skills/run-blazor-admin/driver.js run path/to/step.js                 # your own steps
```

- **`smoke`** prints `[PASS]`/`[FAIL]` for each step: list, create validation, create, edit, change URL,
  publish/unpublish, duplicate, and list + filter. It exits 1 on any failure, and a failing step saves a
  `fail-<step>.png`. Each run **creates two real pages** with unique `smoke-<stamp>` slugs. They're left
  in the database.
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
  list columns, and the unsupported-field placeholder (Example's Dropdown, until phase 4 ports it). Field labels come from MVC metadata, so their casing varies
  ("Short description:"); the step matches labels case-insensitively. It deletes what it creates.
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
- **`run`** loads a module of the form `module.exports = async (page, c) => { ... }`. `page` is a
  Playwright page with `baseURL` already set. `c` provides `settle()`, `shot(name)`, `step(name, fn)`,
  `btn(scope, text)`, `field(scope, label)`, `rows()` and `status()`. See `driver.js`.
- Screenshots and the saved sign-in cookie (`auth-state.json`) go to `$TEMP/blazor-admin-run`
  (override with `OUT_DIR`). Delete `auth-state.json` to force a fresh sign-in.
- Browser console errors, page errors and HTTP 4xx/5xx responses are printed as they happen.
  **Read the screenshots.** A page can render with missing styling and still pass a DOM check.

## Stop

```powershell
Get-Process Dev.Sandbox | Stop-Process -Confirm:$false
```

## Run (human path)

Launch it the same way, open http://localhost:5006/admin-next (the dashboard), and sign in. The AngularJS admin
is at `/<Cofoundry:Admin:DirectoryName>` (`/admin` or `/sandbox-admin`, depending on appsettings), so you
can compare the two side by side.

## Gotchas

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
