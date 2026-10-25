---
name: run-blazor-admin
description: Run, start, smoke-test or screenshot the Blazor admin (/admin-next) in the Dev.Sandbox site, and drive it in headless Chrome. Use to verify a change to Cofoundry.Web.Admin.Blazor actually works in the browser (pages list, create, edit, publish, change URL, duplicate).
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
node .claude/skills/run-blazor-admin/driver.js run path/to/step.js                 # your own steps
```

- **`smoke`** prints `[PASS]`/`[FAIL]` for each step: list, create validation, create, edit, change URL,
  publish/unpublish, duplicate, and list + filter. It exits 1 on any failure, and a failing step saves a
  `fail-<step>.png`. Each run **creates two real pages** with unique `smoke-<stamp>` slugs. They're left
  in the database.
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

Launch it the same way, open http://localhost:5006/admin-next/pages, and sign in. The AngularJS admin
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
- **Signing in through `/admin-next/...` lands you on the AngularJS dashboard**, because the shared login
  ignores the return URL. The driver navigates to its own URL afterwards.

## Troubleshooting

| Symptom | Fix |
|---|---|
| Blank grey page, `[http 404] .../_framework/blazor.web.js` | The host is missing `<RequiresAspNetWebAssets>true</RequiresAspNetWebAssets>`. The SDK only adds the Blazor script when the host itself has `.razor` files. |
| Page body empty, log shows `Authentication schemes cannot be specified for components` | Something put `AuthenticationSchemes` on a component's `[Authorize]`. Use `[AdminBlazorAuthorize]`, which carries the scheme in its policy. |
| `Playwright requires Node.js 18 or higher` | You installed a newer playwright-core. Pin it to `1.32.3`. |
| `strict mode violation: getByLabel(...) resolved to 2 elements` | Add `{ exact: true }`. |
| Input shows literal text like `Value` or `_command.Title` | A `string` component parameter was given a bare name. Write `Value="@x"`. |
| `error CS...` in `sandbox.log`, then the background task exits 1 | Build error. Run `dotnet build src/Cofoundry.Web.Admin.Blazor` to see it clearly. |
