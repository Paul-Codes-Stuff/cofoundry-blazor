// Headless-Chrome driver for the Blazor admin (/admin-next) running in Dev.Sandbox.
// CommonJS + playwright-core 1.32 on purpose: it has to run on Node 14.
//
//   node driver.js smoke              Pages module end-to-end (creates 2 pages per run)
//   node driver.js shot <path> [name] Navigate, wait for the circuit, screenshot (path: admin-next/pages/new)
//   node driver.js run <step.js>      Run a custom step: module.exports = async (page, ctx) => {}
//
// Env: ADMIN_USER, ADMIN_PASS (required), BASE_URL (default http://localhost:5006),
//      OUT_DIR (default <tmp>/blazor-admin-run). playwright-core is resolved via NODE_PATH.

const { chromium } = require('playwright-core');
const fs = require('fs');
const os = require('os');
const path = require('path');

const BASE = process.env.BASE_URL || 'http://localhost:5006';
const OUT = process.env.OUT_DIR || path.join(os.tmpdir(), 'blazor-admin-run');
const STATE = path.join(OUT, 'auth-state.json');
fs.mkdirSync(OUT, { recursive: true });

// Blazor Server renders after the websocket connects, so networkidle alone is too early.
const settle = async (page, ms = 1200) => { await page.waitForLoadState('networkidle'); await page.waitForTimeout(ms); };

async function signIn(page, context) {
  await page.goto('/admin-next/pages');
  if (!page.url().includes('/auth/login')) return;
  if (!process.env.ADMIN_USER || !process.env.ADMIN_PASS) throw new Error('Set ADMIN_USER and ADMIN_PASS');
  await page.fill('#Username', process.env.ADMIN_USER);
  await page.fill('#Password', process.env.ADMIN_PASS);
  await Promise.all([page.waitForNavigation(), page.click('input[type=submit]')]);
  if (page.url().includes('/auth/login')) throw new Error('Sign-in failed (still on login page)');
  await context.storageState({ path: STATE });
  console.log('[signed in]');
}

function makeCtx(page) {
  let failures = 0;
  const shot = async name => {
    const f = path.join(OUT, name + '.png');
    await page.screenshot({ path: f, fullPage: true });
    console.log('[shot] ' + f);
  };
  const step = async (name, fn) => {
    try { await fn(); console.log('[PASS] ' + name); }
    catch (e) { failures++; console.log('[FAIL] ' + name + ': ' + e.message.split('\n')[0]); await shot('fail-' + name.replace(/\W+/g, '-')); }
  };
  // Buttons are <button> (AdminButton) or <input type=submit> (AdminButtonSubmit).
  const btn = (scope, text) => page.locator(`${scope} button:text-is("${text}"), ${scope} input[type=submit][value="${text}"]`).first();
  // exact: "Title:" would otherwise also match "OpenGraph Title:".
  const field = (scope, label) => page.locator(scope).getByLabel(label, { exact: true });
  const rows = async () => (await page.locator('table.table tbody tr').allInnerTexts()).map(s => s.replace(/\s+/g, ' ').trim());
  const status = async () => (await page.locator('.page-body').innerText()).match(/PUBLISHED STATUS:\s*(\S+)/i)?.[1];
  return { shot, step, btn, field, rows, status, settle: ms => settle(page, ms), failures: () => failures };
}

async function smoke(page, c) {
  const stamp = Date.now().toString(36);
  const title = `Smoke ${stamp}`;
  let pageUrl;

  await c.step('list renders', async () => {
    await page.goto('/admin-next/pages'); await c.settle();
    if (!(await page.locator('table.table').count())) throw new Error('no table.table');
    if (!(await page.locator('.page-actions a:has-text("Create")').isVisible())) throw new Error('Create button not visible');
    await c.shot('01-list');
  });

  await c.step('create: validation', async () => {
    await page.click('.page-actions a:has-text("Create")'); await c.settle();
    await c.btn('.page-actions', 'Save Draft').click(); await page.waitForTimeout(800);
    const msgs = await page.locator('.field-validation-error').allInnerTexts();
    if (!msgs.length) throw new Error('no validation messages');
    console.log('       ' + msgs.join(' / '));
  });

  await c.step('create: save draft', async () => {
    await c.field('.page-body', 'Title:').fill(title);
    await c.field('.page-body', 'Title:').blur();   // InputText binds on 'change', which needs a blur
    await c.field('.page-body', 'Page Template:').selectOption({ label: 'General' });
    await c.field('.page-body', 'Directory:').selectOption({ label: '/' });
    await page.waitForTimeout(400);
    const slug = await c.field('.page-body', 'Page Path:').inputValue();
    if (await c.field('.page-body', 'Title:').inputValue() !== title) throw new Error('title box lost its value on re-render');
    if (slug !== `smoke-${stamp}`) throw new Error('auto slug was ' + slug);
    await c.shot('02-create-filled');
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    if (!/\/admin-next\/pages\/\d+$/.test(page.url())) throw new Error('did not land on details: ' + page.url());
    pageUrl = new URL(page.url()).pathname;
    await c.shot('03-details');
  });
  if (!pageUrl) return;

  await c.step('details: edit + save draft', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await c.field('.page-body', 'Title:').fill(title + ' edited');
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    if (!(await page.locator('.page-body').innerText()).includes(title + ' edited')) throw new Error('edit not shown');
  });

  await c.step('change url dialog', async () => {
    await c.btn('.page-actions', 'Change Url').click(); await page.waitForTimeout(1000);
    await c.shot('04-change-url');
    await c.field('.modal', 'Url path:').fill(`smoke-${stamp}-moved`);
    await c.btn('.modal', 'Save').click(); await c.settle(2000);
    const h = await page.locator('h1.page-header').innerText();
    if (!h.includes(`/smoke-${stamp}-moved`)) throw new Error('header is ' + h);
  });

  await c.step('publish + unpublish', async () => {
    await c.btn('.page-actions', 'Publish').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);   // "Yes, publish it"
    if (await c.status() !== 'Published') throw new Error('status after publish: ' + await c.status());
    await c.btn('.page-actions', 'Unpublish').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (await c.status() !== 'Unpublished') throw new Error('status after unpublish: ' + await c.status());
  });

  await c.step('duplicate dialog', async () => {
    await c.btn('.page-actions', 'Duplicate').click(); await page.waitForTimeout(1000);
    await c.shot('05-duplicate');
    await c.field('.modal', 'Title:').fill(`Smoke ${stamp} copy`);
    await c.btn('.modal', 'Create duplicate').click(); await c.settle(2000);
    if (new URL(page.url()).pathname === pageUrl) throw new Error('stayed on original page');
  });

  await c.step('list + filter', async () => {
    await page.goto('/admin-next/pages'); await c.settle();
    const all = await c.rows();
    if (!all.some(r => r.includes(`smoke-${stamp}-moved`))) throw new Error('new page missing from list');
    await page.click('a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await page.locator('.search-filter').getByLabel('Publish Status:', { exact: true }).selectOption({ label: 'Unpublished' });
    await c.btn('.search-filter', 'Apply').click(); await c.settle();
    if ((await c.rows()).some(r => / Published /.test(r))) throw new Error('filter let a Published row through');
    await c.shot('06-list-filtered');
  });
}

(async () => {
  const [cmd, arg, name] = process.argv.slice(2);
  const browser = await chromium.launch({ channel: 'chrome', headless: true });
  const context = await browser.newContext({
    baseURL: BASE,
    viewport: { width: 1400, height: 900 },
    storageState: fs.existsSync(STATE) ? STATE : undefined,
  });
  const page = await context.newPage();
  page.on('console', m => { if (m.type() === 'error') console.log('[console.error] ' + m.text()); });
  page.on('pageerror', e => console.log('[pageerror] ' + e.message));
  page.on('response', r => { if (r.status() >= 400) console.log(`[http ${r.status()}] ${r.url()}`); });

  const c = makeCtx(page);
  try {
    await signIn(page, context);
    if (cmd === 'smoke') await smoke(page, c);
    // Paths are taken without a leading slash: Git Bash rewrites "/admin-next/..." into a Windows path.
    else if (cmd === 'shot') { await page.goto('/' + (arg || 'admin-next/pages').replace(/^\/+/, '')); await c.settle(); await c.shot(name || 'shot'); console.log('[text] ' + (await page.locator('body').innerText()).slice(0, 800)); }
    else if (cmd === 'run') await require(path.resolve(arg))(page, c);
    else throw new Error('usage: node driver.js smoke | shot <path> [name] | run <step.js>');
  } catch (e) {
    console.log('[ERROR] ' + e.message.split('\n')[0]);
    await c.shot('error').catch(() => {});
    process.exitCode = 1;
  }
  await browser.close();
  if (c.failures()) { console.log(`[done] ${c.failures()} step(s) failed`); process.exitCode = 1; }
  else if (!process.exitCode) console.log('[done] ok');
})();
