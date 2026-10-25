// First-run setup against an EMPTY database, so it doesn't use driver.js (which signs in first). Start a
// second sandbox pointed at an empty database (see SKILL.md, "Setup"), then:
//   node .claude/skills/run-blazor-admin/setup-test.js http://localhost:5007
// It checks the auth pages send you to setup, the form validates, setup completes, the new admin can sign
// in, and setup then redirects to the dashboard. The admin it creates uses a throwaway email and password.
const path = require('path');
const { chromium } = require('playwright-core');

const base = process.argv[2] || 'http://localhost:5007';
const outDir = process.env.OUT_DIR || path.join(require('os').tmpdir(), 'blazor-admin-run');
const stamp = Date.now().toString(36);
const email = `setup-${stamp}@example.com`;
const password = `Setup-${stamp}-pw`;
let failures = 0;

(async () => {
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const page = await browser.newPage({ baseURL: base, viewport: { width: 1200, height: 900 } });
  page.on('pageerror', e => console.log('[pageerror] ' + e.message));
  page.on('response', r => { if (r.status() >= 400 && !r.url().endsWith('favicon.ico')) console.log(`[http ${r.status()}] ${r.url()}`); });
  const text = async () => (await page.locator('body').innerText()).replace(/\s+/g, ' ').trim();
  const field = label => page.locator('.setup-container').getByLabel(label, { exact: true });
  const submit = () => Promise.all([page.waitForNavigation(), page.click('input[type=submit]')]);
  const shot = name => page.screenshot({ path: path.join(outDir, name + '.png'), fullPage: true });
  const step = async (name, fn) => {
    try { await fn(); console.log('[PASS] ' + name); }
    catch (e) { failures++; console.log(`[FAIL] ${name}: ${e.message}`); await shot('fail-' + name.replace(/\W+/g, '-')).catch(() => {}); }
  };

  await step('sign in sends you to setup', async () => {
    await page.goto('/admin-next/auth/login');
    if (!/\/admin-next\/setup$/.test(page.url())) throw new Error('went to ' + page.url());
    const t = await text();
    if (!/Site setup/i.test(t) || !/Default User Account/i.test(t)) throw new Error(t.slice(0, 300));
    await shot('setup-1-form');
  });

  await step('setup validates', async () => {
    await submit();
    let t = await text();
    if (!/The Application Name field is required/.test(t) || !/The Email field is required/.test(t) || !/The Password field is required/.test(t)) throw new Error(t.slice(0, 500));
    await field('Application Name:').fill('Setup Test');
    await field('Email:').fill(email);
    await field('Password:').fill(password);
    await field('Confirm password:').fill(password + 'x');
    await submit();
    t = await text();
    if (!/Passwords must match/.test(t)) throw new Error(t.slice(0, 500));
    // The domain's password policy: too short.
    await field('Password:').fill('short'); await field('Confirm password:').fill('short');
    await submit();
    t = await text();
    if (/Setup complete/i.test(t)) throw new Error('a short password was accepted');
    console.log('       ' + t.replace(/.*Default User Account/i, '').slice(0, 200));
    await shot('setup-2-errors');
  });

  await step('setup completes', async () => {
    await field('Application Name:').fill('Setup Test');
    await field('Name:').fill('Setup Admin');
    await field('Email:').fill(email);
    await field('Password:').fill(password); await field('Confirm password:').fill(password);
    await submit();
    const t = await text();
    if (!/Setup complete/i.test(t)) throw new Error(t.slice(0, 500));
    await shot('setup-3-complete');
    await Promise.all([page.waitForNavigation(), page.click('a:text-is("Login")')]);
    if (!/\/admin-next\/auth\/login/.test(page.url())) throw new Error('login link went to ' + page.url());
  });

  await step('new admin signs in', async () => {
    await page.fill('#Username', email); await page.fill('#Password', password);
    await submit();
    await page.waitForTimeout(2500);
    if (/\/auth\/|\/setup/.test(page.url())) throw new Error('sign in failed: ' + page.url());
    const t = await text();
    if (!/Setup Test/.test(t) && !/Welcome/i.test(t)) throw new Error(t.slice(0, 300));
    await shot('setup-4-dashboard');
  });

  await step('setup redirects once set up', async () => {
    await page.goto('/admin-next/setup');
    await page.waitForTimeout(1500);
    if (!/\/admin-next\/dashboard$/.test(page.url())) throw new Error('setup went to ' + page.url());
  });

  await browser.close();
  console.log(failures ? `[done] ${failures} step(s) failed` : '[done] ok');
  process.exit(failures ? 1 : 0);
})();
