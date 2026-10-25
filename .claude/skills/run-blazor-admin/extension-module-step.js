// The admin module extension point (IAdminBlazorModuleRegistration), through the sandbox's Products module:
// a Blazor page that lives in Dev.Sandbox, not in the admin's assembly (Cofoundry/Admin/Blazor). Checks the
// nav links to it, that it loads both directly (endpoint mapping) and by in-app navigation (router), that
// signed-out requests are sent to sign in, and its form: validation, the custom selector field, save, cancel.
// The product is kept in memory, so a restart resets it.
module.exports = async (page, c) => {
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const control = label => page.locator(`.page-body .control-group:has(label:text-is("${label}"))`).first();
  const stamp = Date.now().toString(36).slice(-6);

  await c.step('nav links the module to its Blazor page', async () => {
    await page.goto('/admin-next/dashboard'); await c.settle();
    const href = await page.locator('nav.main-nav a:text-is("Products")').getAttribute('href');
    if (href !== '/admin-next/products') throw new Error('href ' + href);
    await page.locator('nav.main-nav a:text-is("Products")').click({ force: true }); await c.settle();
    if (!page.url().endsWith('/admin-next/products') || !/Test Data/.test(await body())) throw new Error('in-app navigation: ' + page.url());
    if (!(await page.locator('nav.main-nav li.result.active a:text-is("Products")').count())) throw new Error('nav item not active');
  });

  await c.step('direct load', async () => {
    await page.goto('/admin-next/products'); await c.settle();
    const text = await body();
    if (!/TITLE: \S+/i.test(text) || !/TEST SELECTION:/i.test(text)) throw new Error(text);
    await c.shot('ext-1-products');
  });

  await c.step('signed out: sent to sign in', async () => {
    const ctx = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin });
    const res = await ctx.request.get('/admin-next/products', { maxRedirects: 0 });
    await ctx.close();
    const location = res.headers()['location'] || '';
    if (res.status() !== 302 || !/auth\/login/i.test(location)) throw new Error(`${res.status()} ${location}`);
  });

  await c.step('edit: validation, custom field, save', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(500);
    await control('Title:').locator('input').fill(''); await control('Title:').locator('input').blur();
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(500);
    if (!/The Title field is required/.test(await body())) throw new Error('no required message');
    await control('Title:').locator('input').fill(`Product ${stamp}`); await control('Title:').locator('input').blur();
    const options = (await control('Test Selection:').locator('option').allInnerTexts()).map(t => t.trim());
    if (options.join('|') !== 'None|Dog|Cat|Llama') throw new Error('options ' + options.join('|'));
    await control('Test Selection:').locator('select').selectOption({ label: 'Cat' });
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    const text = await body();
    if (!/Changes were saved successfully/.test(text) || !text.includes(`Product ${stamp}`) || !/TEST SELECTION: Cat/i.test(text)) throw new Error(text);
  });

  await c.step('cancel discards changes; saved values survive a reload', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(500);
    await control('Ref:').locator('input').fill('CHANGED'); await control('Ref:').locator('input').blur();
    await c.btn('.page-actions', 'Cancel').click(); await page.waitForTimeout(500);
    if ((await body()).includes('CHANGED')) throw new Error('cancel kept the change');
    await page.reload(); await c.settle();
    if (!(await body()).includes(`Product ${stamp}`)) throw new Error('not saved');
  });
};
