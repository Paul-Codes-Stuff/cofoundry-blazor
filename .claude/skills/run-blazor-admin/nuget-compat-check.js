// Checks the Blazor admin still works with published Cofoundry NuGet packages, so a site can install it
// without upgrading Cofoundry.Core, Cofoundry.Domain and Cofoundry.Web (see "NuGet compatibility" in SKILL.md).
//   node .claude/skills/run-blazor-admin/nuget-compat-check.js            # the latest stable Cofoundry.Web
//   node .claude/skills/run-blazor-admin/nuget-compat-check.js 0.13.2     # a given version
//   KEEP=1 node ...                                                        # keep the copy, for a look
// 1. Compares the repo's Core, Domain and Web (and the plugins' non-Blazor projects) with the commit the
//    package was built from (its nuspec's repository commit): any difference is code the packages lack.
// 2. Copies the new projects (git-tracked files only) to a temp folder and builds each one with
//    -p:CofoundryPackageVersion, which swaps their references to the published projects for those packages,
//    as packing does (see "Packaging" in the admin's README).
// Needs the .NET SDK, git and internet access. Exits 1 on any failure.
const { execFileSync } = require('child_process');
const fs = require('fs');
const os = require('os');
const path = require('path');
const https = require('https');

const repo = execFileSync('git', ['rev-parse', '--show-toplevel'], { encoding: 'utf8' }).trim();
// The projects that are new on this branch, i.e. not published yet. The rest come from NuGet.
const projects = [
  'src/Cofoundry.Web.Admin.Blazor/Cofoundry.Web.Admin.Blazor.csproj',
  'src/Cofoundry.Web.Admin.Api/Cofoundry.Web.Admin.Api.csproj',
  'src/Cofoundry.Plugins/Cofoundry.Plugins.ErrorLogging/Cofoundry.Plugins.ErrorLogging.Admin.Blazor/Cofoundry.Plugins.ErrorLogging.Admin.Blazor.csproj',
  'src/Cofoundry.Plugins/Cofoundry.Plugins.YouTube/Cofoundry.Plugins.YouTube.Admin.Blazor/Cofoundry.Plugins.YouTube.Admin.Blazor.csproj',
  'src/Cofoundry.Plugins/Cofoundry.Plugins.Vimeo/Cofoundry.Plugins.Vimeo.Admin.Blazor/Cofoundry.Plugins.Vimeo.Admin.Blazor.csproj',
];
// Published code the new projects depend on, which this branch must leave as released.
const published = ['src/Cofoundry.Core', 'src/Cofoundry.Domain', 'src/Cofoundry.Web', 'eng/Base.props'];

const get = url => new Promise((resolve, reject) => {
  https.get(url, res => {
    let body = '';
    res.on('data', d => body += d);
    res.on('end', () => res.statusCode === 200 ? resolve(body) : reject(new Error(`${res.statusCode} ${url}`)));
  }).on('error', reject);
});
const nuget = id => `https://api.nuget.org/v3-flatcontainer/${id.toLowerCase()}`;
const git = (...args) => execFileSync('git', args, { cwd: repo, encoding: 'utf8' });

(async () => {
  let failures = 0;
  const fail = message => { failures++; console.log('[FAIL] ' + message); };

  const versions = JSON.parse(await get(nuget('Cofoundry.Web') + '/index.json')).versions;
  const version = process.argv[2] || versions.filter(v => !v.includes('-')).pop();
  if (!versions.includes(version)) throw new Error(`Cofoundry.Web ${version} is not on NuGet`);
  console.log(`Cofoundry ${version}`);

  // 1. Source: the published packages' commit against this branch.
  const nuspec = await get(`${nuget('Cofoundry.Web')}/${version}/cofoundry.web.nuspec`);
  const commit = /<repository [^>]*commit="([0-9a-f]+)"/.exec(nuspec)?.[1];
  if (!commit) {
    console.log('       the nuspec has no commit: source not compared');
  } else {
    let known = true;
    try { git('cat-file', '-e', commit + '^{commit}'); } catch { known = false; }
    if (!known) {
      console.log(`       commit ${commit.slice(0, 8)} isn't in this clone (fetch cofoundry-cms/cofoundry): source not compared`);
    } else {
      const pluginsPublished = git('ls-files', 'src/Cofoundry.Plugins').split('\n')
        .filter(f => f && !/\.Admin\.Blazor\//.test(f)).map(f => f.split('/').slice(0, 4).join('/'));
      const paths = [...published, ...new Set(pluginsPublished)];
      const changed = git('diff', '--stat', commit, 'HEAD', '--', ...paths).replace(/\s+$/, '');
      if (changed) fail(`published projects changed since ${version} (${commit.slice(0, 8)}):\n${changed}`);
      else console.log(`[PASS] source: Core, Domain, Web and the plugins match ${version} (${commit.slice(0, 8)})`);
    }
  }

  // 2. Build: the new projects against the packages.
  const out = fs.mkdtempSync(path.join(os.tmpdir(), 'cofoundry-nuget-compat-'));
  const dirs = projects.map(p => path.posix.dirname(p));
  for (const file of git('ls-files', '--', ...dirs).split('\n').filter(Boolean)) {
    fs.mkdirSync(path.join(out, path.dirname(file)), { recursive: true });
    fs.copyFileSync(path.join(repo, file), path.join(out, file));
  }
  // The repo's own props would import eng/Base.props; take only its target framework.
  const framework = /<TargetFramework>([^<]+)</.exec(fs.readFileSync(path.join(repo, 'eng/Base.props'), 'utf8'))[1];
  fs.writeFileSync(path.join(out, 'Directory.Build.props'), `<Project>
  <PropertyGroup>
    <TargetFramework>${framework}</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
`);

  for (const project of projects) {
    const name = path.basename(project, '.csproj');
    try {
      execFileSync('dotnet', ['build', path.join(out, project), `-p:CofoundryPackageVersion=${version}`, '--nologo', '-v', 'q', '-clp:ErrorsOnly'], { encoding: 'utf8', stdio: 'pipe' });
      console.log(`[PASS] build: ${name}`);
    } catch (e) {
      const errors = [...new Set(((e.stdout || '') + (e.stderr || '')).split('\n').filter(l => /error/i.test(l)).map(l => l.replace(/\s*\[[^\]]*\]\s*$/, '').trim()))];
      fail(`build: ${name}\n       ${errors.slice(0, 20).join('\n       ')}`);
    }
  }

  if (process.env.KEEP) console.log('       kept: ' + out);
  else fs.rmSync(out, { recursive: true, force: true });
  console.log(failures ? `[done] ${failures} failed` : '[done] ok');
  process.exit(failures ? 1 : 0);
})().catch(e => { console.log('[FAIL] ' + e.message); process.exit(1); });
