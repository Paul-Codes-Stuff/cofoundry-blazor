// The plugins' Blazor packages, installed in the sandbox next to their AngularJS packages:
// - Error Logging (Cofoundry.Plugins.ErrorLogging.Admin.Blazor): logs an error through the sandbox's test/error
//   route, then checks the module is listed once in each admin's nav, and its list, filter and details pages.
// - YouTube (Cofoundry.Plugins.YouTube.Admin.Blazor): the [YouTube] field on the dev-only Field Tests entity
//   (SIMFLD): the picker dialog's id/url parsing, title and description, save, cancel and remove. The sandbox
//   has no YouTube API key, so nothing is looked up on YouTube and only the id comes back from the check.
// Logged errors stay in the database (the plugin has no delete); the custom entity is deleted.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const control = label => page.locator(`.page-body .control-group:has(label:text-matches("^${label}$", "i"))`).first();
  const top = () => page.locator('.modal').last();

  // Error Logging

  const errorStamp = `errlog-${stamp}`;
  await c.step('errors: log one', async () => {
    const res = await page.request.get(`/test/error?stamp=${errorStamp}`);
    if (res.status() !== 500) throw new Error('status ' + res.status());
  });

  await c.step('errors: listed once in both navs', async () => {
    await page.goto('/admin-next/dashboard'); await c.settle();
    const links = page.locator('nav.main-nav a:text-is("Error Log")');
    if ((await links.count()) !== 1) throw new Error('Blazor nav links: ' + await links.count());
    if ((await links.getAttribute('href')) !== '/admin-next/errors') throw new Error('href ' + await links.getAttribute('href'));
    // The AngularJS package also describes the module, so the Blazor one must not add a second.
    const res = await page.request.get('/admin');
    const html = await res.text();
    const count = (html.match(/>\s*Error Log\s*</g) || []).length;
    if (count !== 1) throw new Error('AngularJS nav entries: ' + count);
  });

  await c.step('errors: list and filter', async () => {
    // The Settings sub-nav is hidden until hovered, so the click is dispatched.
    await page.locator('nav.main-nav a:text-is("Error Log")').dispatchEvent('click'); await c.settle();
    if (!page.url().endsWith('/admin-next/errors')) throw new Error(page.url());
    const message = `Error logging test ${errorStamp}`;
    if (!(await page.locator(`table.table a:text-is("${message}")`).count())) throw new Error('error not listed: ' + (await body()).slice(0, 300));
    await c.shot('plugins-1-errors');
    await c.btn('.page-actions', 'Filter').click(); await page.waitForTimeout(300);
    await c.field('.page-actions', 'Text:').fill(errorStamp); await c.field('.page-actions', 'Text:').blur();
    await c.btn('.page-actions', 'Apply').click(); await c.settle(800);
    const rows = await c.rows();
    if (rows.length !== 1 || !rows[0].includes(message) || !rows[0].includes('/test/error')) throw new Error(rows.join(' | '));
    await c.btn('.page-actions', 'Filter').click(); await page.waitForTimeout(300);
    await c.field('.page-actions', 'Text:').fill(`nothing-${stamp}`); await c.field('.page-actions', 'Text:').blur();
    await c.btn('.page-actions', 'Apply').click(); await c.settle(800);
    if (!/Hooray! No Errors!/.test(await body())) throw new Error('empty message missing');
  });

  await c.step('errors: details', async () => {
    await page.goto('/admin-next/errors'); await c.settle();
    await page.click(`table.table a:text-is("Error logging test ${errorStamp}")`); await c.settle();
    const id = (page.url().match(/\/admin-next\/errors\/(\d+)$/) || [])[1];
    if (!id) throw new Error(page.url());
    const header = (await page.locator('.page-header').innerText()).replace(/\s+/g, ' ');
    if (!header.includes(`Id:${id}`) || !header.includes('Errors')) throw new Error('header ' + header);
    const text = await body();
    for (const expected of [`MESSAGE: Error logging test ${errorStamp}`, 'URL: GET http://localhost', `QUERYSTRING: ?stamp=${errorStamp}`, 'Other', 'Stack Trace']) {
      if (!text.toUpperCase().includes(expected.toUpperCase())) throw new Error(`"${expected}" missing: ` + text.slice(0, 500));
    }
    if (!/at Dev\.Sandbox\.TestController\.Error/.test(await page.locator('.page-body pre').innerText())) throw new Error('no stack trace');
    await c.shot('plugins-2-error-details');
    await page.goto('/admin-next/errors/2147483647'); await c.settle();
    if (!/Sorry, that error could not be found/.test(await body())) throw new Error('not found message missing');
  });

  await c.step('errors: signed out is sent to sign in', async () => {
    const ctx = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin });
    const res = await ctx.request.get('/admin-next/errors', { maxRedirects: 0 });
    await ctx.close();
    if (res.status() !== 302 || !/auth\/login/i.test(res.headers()['location'] || '')) throw new Error(res.status());
  });

  // YouTube

  const list = '/admin-next/custom-entities/SIMFLD';
  const title = `YouTube ${stamp}`;
  let id;
  const stored = async () => {
    const res = await page.request.get(`/admin/api/custom-entities/${id}`);
    const json = await res.json();
    return (json.data || json).latestVersion.model.video;
  };
  const video = () => control('Video:');
  const dialogBtn = text => top().locator(`.page-actions button:text-is("${text}")`);
  const idInput = () => top().locator('input[placeholder="enter a youtube url or id"]');
  const checkId = async value => {
    await idInput().fill(value); await idInput().blur();
    await top().locator('.form-field-input-actions button:text-is("Ok")').click(); await page.waitForTimeout(600);
  };

  await c.step('youtube: empty field', async () => {
    await page.goto(`${list}/new`); await c.settle();
    await c.field('.page-body', 'Title:').fill(title); await c.field('.page-body', 'Title:').blur();
    await control('Date:').locator('input').fill('2024-05-06'); await control('Date:').locator('input').blur();
    await control('Enum radio:').locator('label:text-is("Red")').click();
    if (!(await video().locator('img[title="Not set"]').count())) throw new Error('no placeholder image');
    if (!(await video().locator('button[title="Select"]').count())) throw new Error('no Select button');
    if (await video().locator('button[title="Remove"]').count()) throw new Error('Remove shown with no video');
  });

  await c.step('youtube: picker parses ids and urls', async () => {
    await video().locator('button[title="Select"]').click(); await page.waitForTimeout(800);
    if (!/YouTube Video/.test(await top().innerText())) throw new Error('dialog not open');
    if (!(await dialogBtn('Ok').isDisabled())) throw new Error('Ok enabled with no video');
    await checkId('not a video');
    if (!/The url\/id is invalid/.test(await top().innerText())) throw new Error('no invalid message');
    await checkId('https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10');
    const text = await top().innerText();
    if (!/dQw4w9WgXcQ/.test(text) || /invalid/.test(text)) throw new Error(text);
    if (!(await top().locator('button:text-is("Change")').count())) throw new Error('id not accepted');
    const src = await top().locator('iframe').getAttribute('src');
    if (src !== 'https://www.youtube-nocookie.com/embed/dQw4w9WgXcQ') throw new Error('preview ' + src);
    // A short url, through Change.
    await top().locator('button:text-is("Change")').click(); await page.waitForTimeout(300);
    await checkId('https://youtu.be/9bZkp7q19f0');
    if (!(await top().locator('iframe').getAttribute('src')).endsWith('/9bZkp7q19f0')) throw new Error('short url not parsed');
    await top().getByLabel('Title:', { exact: true }).fill(`Video ${stamp}`); await top().getByLabel('Title:', { exact: true }).blur();
    await top().getByLabel('Description:', { exact: true }).fill('A test video'); await top().getByLabel('Description:', { exact: true }).blur();
    await c.shot('plugins-3-youtube-picker');
    await dialogBtn('Ok').click(); await page.waitForTimeout(600);
    if (await page.locator('.modal').count()) throw new Error('dialog still open');
    if (!(await video().locator('iframe[src$="/9bZkp7q19f0"]').count()) || !(await video().innerText()).includes(`Video ${stamp}`)) throw new Error('field not updated');
  });

  await c.step('youtube: save', async () => {
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    id = (page.url().match(/\/SIMFLD\/(\d+)$/) || [])[1];
    if (!id) throw new Error('not saved: ' + (await body()).slice(0, 300));
    const v = await stored();
    if (!v || v.id !== '9bZkp7q19f0' || v.title !== `Video ${stamp}` || v.description !== 'A test video') throw new Error(JSON.stringify(v));
    if (!(await video().locator('iframe').count()) || await video().locator('button').count()) throw new Error('read-only view');
    await c.shot('plugins-4-youtube-view');
  });

  await c.step('youtube: cancel keeps it; remove clears it', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1200);
    await video().locator('button[title="Change"]').click(); await page.waitForTimeout(800);
    if ((await top().getByLabel('Title:', { exact: true }).inputValue()) !== `Video ${stamp}`) throw new Error('title not loaded');
    await top().getByLabel('Title:', { exact: true }).fill('Changed'); await top().getByLabel('Title:', { exact: true }).blur();
    await dialogBtn('Cancel').click(); await page.waitForTimeout(500);
    if ((await video().innerText()).includes('Changed')) throw new Error('cancel kept the change');
    await video().locator('button[title="Remove"]').click(); await page.waitForTimeout(400);
    if (!(await video().locator('img[title="Not set"]').count())) throw new Error('not removed');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const v = await stored();
    if (v != null) throw new Error(JSON.stringify(v));
  });

  await c.step('youtube: cleanup', async () => {
    await page.goto(list); await c.settle();
    if (!(await page.locator(`table.table a:text-is("${title}")`).count())) return;
    await page.click(`table.table a:text-is("${title}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith(list)) throw new Error('delete failed');
  });
};
