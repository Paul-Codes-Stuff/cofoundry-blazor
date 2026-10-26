// Visual regression check for stylesheet changes. Screenshots a set of admin screens and compares them pixel by
// pixel with a baseline taken before the change. Saves nothing.
//   STYLES_BASELINE=1 node driver.js run .claude/skills/run-blazor-admin/styles-step.js   # before the change
//   node driver.js run .claude/skills/run-blazor-admin/styles-step.js                      # after: compares
// Screens go to $OUT_DIR/styles-baseline and styles-current; differing ones also get a styles-diff-<name>.png
// with changed pixels in red. Relative times ("3 minutes ago") and new data change pixels too, so read the diffs.
const fs = require('fs');
const os = require('os');
const path = require('path');

const OUT = process.env.OUT_DIR || path.join(os.tmpdir(), 'blazor-admin-run');
const isBaseline = !!process.env.STYLES_BASELINE;
const dir = path.join(OUT, isBaseline ? 'styles-baseline' : 'styles-current');
// Share of pixels allowed to differ, for antialiasing.
const tolerance = 0.002;

module.exports = async (page, c) => {
  fs.mkdirSync(dir, { recursive: true });
  const firstLink = async (listPath, selector = 'table.table tbody tr td a') => {
    await page.goto(listPath); await c.settle();
    return page.locator(selector).first().getAttribute('href');
  };

  const screens = [
    ['dashboard', '/admin-next/dashboard'],
    ['pages', '/admin-next/pages'],
    ['pages-new', '/admin-next/pages/new'],
    ['page-details', async () => firstLink('/admin-next/pages')],
    ['images', '/admin-next/images'],
    ['images-new', '/admin-next/images/new'],
    ['documents', '/admin-next/documents'],
    ['directories', '/admin-next/directories'],
    ['page-templates', '/admin-next/page-templates'],
    ['settings', '/admin-next/settings'],
    ['roles', '/admin-next/roles'],
    ['roles-new', '/admin-next/roles/new'],
    ['users', '/admin-next/users/COF'],
    ['account', '/admin-next/account'],
    ['field-tests-new', '/admin-next/custom-entities/SIMFLD/new'],
  ];

  const take = async (name, target) => {
    const url = typeof target === 'function' ? await target() : target;
    await page.goto(url); await c.settle(1500);
    await page.screenshot({ path: path.join(dir, name + '.png'), fullPage: true });
  };

  await c.step('screens', async () => {
    for (const [name, target] of screens) await take(name, target);

    // A dialog over a page, and an open search-as-you-type dropdown.
    await page.goto(await firstLink('/admin-next/pages')); await c.settle();
    await c.btn('.page-actions', 'Duplicate').click(); await page.waitForTimeout(1200);
    await page.screenshot({ path: path.join(dir, 'dialog.png'), fullPage: true });
    await page.goto('/admin-next/custom-entities/SIMBLP/new'); await c.settle();
    await page.locator('.control-group:has(label:text-is("Author:")) input.ui-select-search').click(); await page.waitForTimeout(1200);
    await page.screenshot({ path: path.join(dir, 'filtered-dropdown.png'), fullPage: true });

    // Signed out.
    const anon = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin, viewport: { width: 1400, height: 900 } });
    const anonPage = await anon.newPage();
    await anonPage.goto('/admin-next/auth/login'); await anonPage.waitForTimeout(800);
    await anonPage.screenshot({ path: path.join(dir, 'login.png'), fullPage: true });
    await anon.close();
  });

  if (isBaseline) {
    console.log('[baseline] ' + dir);
    return;
  }

  const baselineDir = path.join(OUT, 'styles-baseline');
  for (const file of fs.readdirSync(dir).filter(f => f.endsWith('.png'))) {
    await c.step('compare ' + file, async () => {
      const before = path.join(baselineDir, file);
      if (!fs.existsSync(before)) throw new Error('no baseline; run with STYLES_BASELINE=1 first');
      const toData = f => 'data:image/png;base64,' + fs.readFileSync(f).toString('base64');
      const result = await page.evaluate(async ([a, b]) => {
        const load = src => new Promise((res, rej) => { const i = new Image(); i.onload = () => res(i); i.onerror = rej; i.src = src; });
        const [ia, ib] = await Promise.all([load(a), load(b)]);
        const w = Math.max(ia.width, ib.width), h = Math.max(ia.height, ib.height);
        const pixels = img => { const cv = document.createElement('canvas'); cv.width = w; cv.height = h; const ctx = cv.getContext('2d'); ctx.drawImage(img, 0, 0); return ctx.getImageData(0, 0, w, h); };
        const da = pixels(ia), db = pixels(ib);
        const out = document.createElement('canvas'); out.width = w; out.height = h;
        const octx = out.getContext('2d'); octx.drawImage(ib, 0, 0);
        const diff = octx.getImageData(0, 0, w, h);
        let changed = 0;
        for (let i = 0; i < da.data.length; i += 4) {
          const d = Math.abs(da.data[i] - db.data[i]) + Math.abs(da.data[i + 1] - db.data[i + 1]) + Math.abs(da.data[i + 2] - db.data[i + 2]);
          if (d > 24) { changed++; diff.data[i] = 255; diff.data[i + 1] = 0; diff.data[i + 2] = 0; diff.data[i + 3] = 255; }
        }
        octx.putImageData(diff, 0, 0);
        return { changed, total: w * h, sizes: `${ia.width}x${ia.height} -> ${ib.width}x${ib.height}`, png: changed ? out.toDataURL('image/png').split(',')[1] : null };
      }, [toData(before), toData(path.join(dir, file))]);

      const share = result.changed / result.total;
      if (result.png) fs.writeFileSync(path.join(OUT, 'styles-diff-' + file), Buffer.from(result.png, 'base64'));
      console.log(`       ${file}: ${(share * 100).toFixed(3)}% changed (${result.sizes})`);
      if (share > tolerance) throw new Error(`${(share * 100).toFixed(2)}% of pixels differ, see styles-diff-${file}`);
    });
  }
};
