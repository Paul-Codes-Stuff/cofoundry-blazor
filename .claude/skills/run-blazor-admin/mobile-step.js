// Small screens (Styles/Blazor/_mobile.scss, js/mobile-nav.js). At phone and tablet widths no page scrolls
// sideways; on a phone the nav is hidden behind the menu button, which opens it, and a module link, the backdrop
// or Escape closes it, on interactive and static SSR pages; dialogs cover the button. Saves nothing.
// ADMIN_ROOT=/admin for a sandbox with the Blazor admin in the AngularJS admin's place.
const ROOT = process.env.ADMIN_ROOT || '/admin-next';

module.exports = async (page, c) => {
  const pages = ['/dashboard', '/pages', '/pages/new', '/images', '/images/new', '/documents', '/directories',
    '/settings', '/roles', '/roles/new', '/users/COF', '/users/COF/new', '/page-templates', '/custom-entities/SIMFLD',
    '/custom-entities/SIMFLD/new', '/account', '/account?edit=true'];

  // The page's own width: a table may be wider, inside its scrolling .table-wrap.
  const overflow = () => page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
  const navRight = () => page.evaluate(() => document.querySelector('.main-nav').getBoundingClientRect().right);
  const isOpen = () => page.evaluate(() => document.documentElement.classList.contains('nav-open'));
  const toggle = () => page.locator('.mobile-nav-toggle');

  const checkNoOverflow = async width => {
    await page.setViewportSize({ width, height: 844 });
    const wide = [];
    for (const url of pages) {
      await page.goto(ROOT + url); await c.settle();
      const by = await overflow();
      if (by > 0) wide.push(`${url} (${by}px)`);
    }
    // Details pages, from the first row of their lists.
    for (const list of ['/pages', '/images', '/roles', '/directories']) {
      await page.goto(ROOT + list); await c.settle();
      const href = await page.locator('table.table tbody tr td a').first().getAttribute('href').catch(() => null);
      if (!href) continue;
      await page.goto(href); await c.settle();
      const by = await overflow();
      if (by > 0) wide.push(`${list} details (${by}px)`);
    }
    if (wide.length) throw new Error('scrolls sideways: ' + wide.join(', '));
  };

  await c.step('phone: no page scrolls sideways', () => checkNoOverflow(390));

  await c.step('phone: nav hidden, menu button opens it', async () => {
    await page.goto(ROOT + '/pages'); await c.settle();
    if (await navRight() > 0) throw new Error('nav showing');
    await toggle().click(); await page.waitForTimeout(400);
    if (!await isOpen() || await navRight() <= 0) throw new Error('nav not opened');
    if (await toggle().getAttribute('aria-expanded') !== 'true') throw new Error('aria-expanded not set');
    if (!await page.locator('.main-nav .result a', { hasText: 'Images' }).isVisible()) throw new Error('modules not shown');
    await c.shot('mobile-nav-open');
  });

  await c.step('phone: module link navigates and closes the nav', async () => {
    await page.locator('.main-nav .result a', { hasText: 'Images' }).click(); await c.settle();
    if (!page.url().includes('/images')) throw new Error('not navigated: ' + page.url());
    if (await isOpen()) throw new Error('nav still open');
    await page.waitForTimeout(400);
    if (await navRight() > 0) throw new Error('nav showing');
  });

  await c.step('phone: backdrop and Escape close the nav', async () => {
    await toggle().click(); await page.waitForTimeout(400);
    await page.mouse.click(380, 400); await page.waitForTimeout(400);
    if (await isOpen()) throw new Error('backdrop left it open');
    await toggle().click(); await page.keyboard.press('Escape');
    if (await isOpen()) throw new Error('Escape left it open');
  });

  await c.step('phone: menu on a static SSR page', async () => {
    await page.goto(ROOT + '/account'); await c.settle();
    await toggle().click(); await page.waitForTimeout(400);
    if (!await isOpen() || await navRight() <= 0) throw new Error('nav not opened');
  });

  await c.step('phone: form and actions', async () => {
    await page.goto(ROOT + '/pages/new'); await c.settle();
    const actions = await page.locator('.page-actions').boundingBox();
    const form = await page.locator('.page-body').boundingBox();
    if (actions.y + actions.height > form.y) throw new Error('actions not above the form');
    await c.shot('mobile-pages-new');
    await page.goto(ROOT + '/custom-entities/SIMFLD/new'); await c.settle();
    await c.shot('mobile-field-tests');
  });

  await c.step('phone: dialog covers the menu button', async () => {
    await page.goto(ROOT + '/pages'); await c.settle();
    await page.goto(await page.locator('table.table tbody tr td a').first().getAttribute('href')); await c.settle();
    await c.btn('.page-actions', 'Change Url').click(); await page.waitForTimeout(800);
    const top = await page.evaluate(() => { const r = document.querySelector('.mobile-nav-toggle').getBoundingClientRect();
      return document.elementFromPoint(r.x + r.width / 2, r.y + r.height / 2).closest('.modal') != null; });
    if (!top) throw new Error('menu button above the dialog');
    if (await overflow() > 0) throw new Error('dialog scrolls sideways');
    await c.shot('mobile-dialog');
  });

  await c.step('phone: small dialog fits the width', async () => {
    await page.locator('.modal .close').last().click(); await page.waitForTimeout(600);
    await c.btn('.page-actions', 'Publish').click(); await page.waitForTimeout(800);
    const box = await page.locator('.modal.modal--show').last().boundingBox();
    if (box.x < 0 || box.x + box.width > 390) throw new Error('dialog off screen: ' + JSON.stringify(box));
    await page.screenshot({ path: require('path').join(process.env.OUT_DIR || require('path').join(require('os').tmpdir(), 'blazor-admin-run'), 'mobile-confirm.png') });
    await page.locator('.modal .close').last().click(); await page.waitForTimeout(600);
  });

  await c.step('phone: search filter fits the width', async () => {
    await page.goto(ROOT + '/pages'); await c.settle();
    await page.click('a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    const box = await page.locator('.search-filter').boundingBox();
    if (box.x < 0 || box.x + box.width > 390) throw new Error('filter off screen: ' + JSON.stringify(box));
    await page.screenshot({ path: require('path').join(process.env.OUT_DIR || require('path').join(require('os').tmpdir(), 'blazor-admin-run'), 'mobile-filter.png') });
  });

  await c.step('phone: signed-out pages fit', async () => {
    const context = await page.context().browser().newContext({ baseURL: process.env.BASE_URL || 'http://localhost:5006', viewport: { width: 390, height: 844 } });
    const signedOut = await context.newPage();
    try {
      for (const url of ['/auth/login', '/auth/forgot-password']) {
        await signedOut.goto(ROOT + url);
        const by = await signedOut.evaluate(() => document.documentElement.scrollWidth - window.innerWidth);
        if (by > 0) throw new Error(`${url} scrolls sideways by ${by}px`);
      }
    } finally {
      await context.close();
    }
  });

  await c.step('tablet: no page scrolls sideways, nav showing', async () => {
    await checkNoOverflow(768);
    if (await toggle().isVisible()) throw new Error('menu button showing');
    if (await navRight() <= 0) throw new Error('nav hidden');
    await c.shot('mobile-tablet');
  });

  await c.step('desktop: no menu button', async () => {
    await page.setViewportSize({ width: 1400, height: 900 });
    await page.goto(ROOT + '/pages'); await c.settle();
    if (await toggle().isVisible()) throw new Error('menu button showing');
  });
};
