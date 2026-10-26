// The Blazor admin under a non-default Cofoundry:AdminBlazor:DirectoryName. Start a second sandbox with the
// setting (see SKILL.md, "Directory name"), then:
//   ADMIN_USER=... ADMIN_PASS=... node .claude/skills/run-blazor-admin/directory-name-test.js http://localhost:5008 cms
// It doesn't use driver.js, whose steps assume /admin-next. Saves nothing.
const path = require('path');
const { chromium } = require('playwright-core');

const base = process.argv[2] || 'http://localhost:5008';
const prefix = '/' + (process.argv[3] || 'cms');
const outDir = process.env.OUT_DIR || path.join(require('os').tmpdir(), 'blazor-admin-run');
let failures = 0;

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const context = await browser.newContext({ baseURL: base, viewport: { width: 1400, height: 900 } });
  const page = await context.newPage();
  const problems = [];
  page.on('pageerror', e => problems.push('[pageerror] ' + e.message));
  page.on('console', m => { if (m.type() === 'error' && !/favicon/.test(m.location().url)) problems.push('[console.error] ' + m.text()); });
  page.on('response', r => { if (r.status() >= 400 && !r.url().endsWith('favicon.ico')) problems.push(`[http ${r.status()}] ${r.url()}`); });
  const settle = async (ms = 1500) => { await page.waitForLoadState('networkidle'); await page.waitForTimeout(ms); };
  const text = async () => (await page.locator('body').innerText()).replace(/\s+/g, ' ').trim();
  const shot = name => page.screenshot({ path: path.join(outDir, name + '.png'), fullPage: true });
  const step = async (name, fn) => {
    try {
      await fn();
      if (problems.length) throw new Error(problems.join(' | '));
      console.log('[PASS] ' + name);
    } catch (e) {
      failures++; console.log(`[FAIL] ${name}: ${e.message}`);
      await shot('fail-' + name.replace(/\W+/g, '-')).catch(() => {});
    }
    problems.length = 0;
  };

  await step('signed out: sent to the prefixed sign in', async () => {
    await page.goto(prefix + '/pages');
    if (!page.url().startsWith(base + prefix + '/auth/login')) throw new Error('went to ' + page.url());
    const color = await page.evaluate(() => getComputedStyle(document.body).backgroundColor);
    if (color === 'rgba(0, 0, 0, 0)') throw new Error('admin.css not applied');
  });

  await step('sign in', async () => {
    await page.fill('#Username', process.env.ADMIN_USER); await page.fill('#Password', process.env.ADMIN_PASS);
    await Promise.all([page.waitForNavigation(), page.click('input[type=submit]')]);
    await settle();
    if (!page.url().startsWith(base + prefix + '/pages')) throw new Error('went to ' + page.url());
    if (!/Pages/.test(await text())) throw new Error((await text()).slice(0, 300));
  });

  await step('nav links and in-app navigation use the prefix', async () => {
    const hrefs = await page.locator('nav a[href]').evaluateAll(as => as.map(a => a.getAttribute('href')));
    const admin = hrefs.filter(h => h.startsWith('/admin-next'));
    if (admin.length) throw new Error('links to /admin-next: ' + admin.join(', '));
    // The link is in a hover menu; a script click still goes through Blazor's navigation interception.
    await page.locator(`nav a[href="${prefix}/settings"]`).first().evaluate(a => a.click()); await settle();
    if (page.url() !== base + prefix + '/settings') throw new Error('went to ' + page.url());
    if (!/Application Name/i.test(await text())) throw new Error((await text()).slice(0, 300));
    await page.goBack(); await settle();
    if (!page.url().startsWith(base + prefix + '/pages')) throw new Error('back went to ' + page.url());
    await page.locator('table.table tbody tr td a').first().click(); await settle();
    if (!new RegExp(prefix + '/pages/\\d+$').test(page.url())) throw new Error('details went to ' + page.url());
    await shot('directory-name-1-details');
  });

  await step('dialog opens and closes', async () => {
    await page.locator('.page-actions button:has-text("Duplicate")').click(); await page.waitForTimeout(1200);
    if (await page.locator('.modal.modal--show').count() !== 1) throw new Error('no dialog');
    await page.locator('.modal button:has-text("Cancel")').click(); await page.waitForTimeout(800);
    if (await page.locator('.modal').count() !== 0) throw new Error('dialog still open');
  });

  await step('direct loads, static SSR page and session check', async () => {
    await page.goto(prefix); await settle();
    if (!/Welcome|Dashboard/i.test(await text())) throw new Error('root: ' + (await text()).slice(0, 200));
    await page.goto(prefix + '/account'); await settle(800);
    if (!/My Profile/.test(await text())) throw new Error('account: ' + (await text()).slice(0, 200));
    const session = await page.request.get(prefix + '/auth/session');
    if (session.status() !== 204) throw new Error('session check ' + session.status());
  });

  await step('the default prefix is not mapped', async () => {
    const res = await page.request.get('/admin-next/pages', { maxRedirects: 0 });
    const body = res.status() === 200 ? await res.text() : '';
    if (/blazor\.web\.js/.test(body)) throw new Error('/admin-next still serves the admin');
    problems.length = 0;
  });

  await step('sign out', async () => {
    await page.goto(prefix + '/auth/logout'); await settle(800);
    if (!page.url().startsWith(base + prefix + '/auth/login')) throw new Error('went to ' + page.url());
  });

  await browser.close();
  console.log(failures ? `[done] ${failures} step(s) failed` : '[done] ok');
  process.exit(failures ? 1 : 0);
})();
