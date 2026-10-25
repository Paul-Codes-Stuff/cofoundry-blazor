// Images and Documents end-to-end: upload (small + oversized image, document), validation, edit,
// replace file, filter, delete. Test files are generated in the browser so nothing is committed.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const text = async sel => (await page.locator(sel).innerText()).replace(/\s+/g, ' ').trim();
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = '#fff'; ctx.fillRect(w / 4, h / 4, w / 2, h / 2);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const fileInput = () => page.locator('.form-field-image-upload input[type=file]');
  const findRow = async title => (await c.rows()).find(r => r.includes(title));
  const openRow = async title => { await page.click(`table.table a:text-is("${title}")`); await c.settle(); };

  /* IMAGES */

  await c.step('images list renders', async () => {
    await page.goto('/admin-next/images'); await c.settle();
    if (!(await page.locator('table.table').count())) throw new Error('no table');
    await c.shot('img-1-list');
  });

  await c.step('upload: empty submit shows file required', async () => {
    await page.click('.page-actions a:has-text("Upload Image")'); await c.settle();
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    const msgs = (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
    console.log('       ' + msgs);
    if (!/File/.test(msgs)) throw new Error('no file validation: ' + msgs);
    await c.shot('img-2-validation');
  });

  const smallTitle = `Smoke-img-${stamp}`;
  await c.step('upload small png: title from name, anchor shown, saves', async () => {
    await fileInput().setInputFiles({ name: `smoke-img-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(400, 300, '#c33') });
    await page.waitForTimeout(1500);
    const title = await c.field('.page-body', 'Title:').inputValue();
    if (title !== smallTitle) throw new Error('title was ' + title);
    if (!(await page.getByLabel('Cropped Anchor Location:', { exact: true }).count())) throw new Error('no anchor field');
    if (await page.locator('text=will be resized').count()) throw new Error('small image claims resize');
    await c.shot('img-3-chosen');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error('not back on list: ' + page.url());
    const row = await findRow(smallTitle);
    console.log('       ' + row);
    if (!row || !row.includes('400 x 300')) throw new Error('row missing or wrong size');
  });

  const bigTitle = `Smoke-big-${stamp}`;
  await c.step('upload oversized png: resized in browser to 3200 wide', async () => {
    await page.goto('/admin-next/images/new'); await c.settle();
    await fileInput().setInputFiles({ name: `smoke-big-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(4000, 2000, '#36c') });
    await page.waitForTimeout(3000);
    const body = await text('.page-body');
    if (!/resized to 3200x1600/.test(body)) throw new Error('no resize message: ' + body.slice(0, 300));
    await c.shot('img-4-resized');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2500);
    const row = await findRow(bigTitle);
    console.log('       ' + row);
    if (!row || !row.includes('3200 x 1600')) throw new Error('row missing or not resized');
  });

  await c.step('thumbnails load', async () => {
    const broken = await page.locator('table.table img').evaluateAll(imgs => imgs.filter(i => !i.complete || i.naturalWidth === 0).map(i => i.src));
    if (broken.length) throw new Error('broken thumbnails: ' + broken.join(', '));
    await c.shot('img-5-list-with-uploads');
  });

  await c.step('filter by width', async () => {
    await page.click('a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await page.locator('.search-filter').getByLabel('Width:', { exact: true }).fill('3200');
    await c.btn('.search-filter', 'Apply').click(); await c.settle();
    const rows = await c.rows();
    if (!rows.some(r => r.includes(bigTitle)) || rows.some(r => r.includes(smallTitle))) throw new Error('filter wrong: ' + rows.join(' | '));
    await page.click('a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await c.btn('.search-filter', 'Clear').click(); await c.settle();
  });

  await c.step('details: edit title + replace file', async () => {
    await openRow(smallTitle);
    const body = await text('.page-body');
    if (!/Path:\s*\/assets\/images\//i.test(body)) throw new Error('no path: ' + body.slice(0, 300));
    await c.shot('img-6-details');
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await c.field('.page-body', 'Title:').fill(smallTitle + ' edited'); await c.field('.page-body', 'Title:').blur();
    await fileInput().setInputFiles({ name: 'replacement.png', mimeType: 'image/png', buffer: await makePng(640, 480, '#3a3') });
    await page.waitForTimeout(1500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const after = await text('.page-body');
    if (!/saved successfully/.test(after)) throw new Error('no success: ' + after.slice(0, 300));
    if ((await text('h1')) .indexOf('edited') < 0 && !(await page.locator('text=' + smallTitle + ' edited').count())) throw new Error('title not updated');
    await c.shot('img-7-saved');
    await page.goto('/admin-next/images'); await c.settle();
    const row = await findRow(smallTitle + ' edited');
    console.log('       ' + row);
    if (!row || !row.includes('640 x 480')) throw new Error('file not replaced');
  });

  for (const title of [smallTitle + ' edited', bigTitle]) {
    await c.step(`delete image ${title}`, async () => {
      await page.goto('/admin-next/images'); await c.settle();
      await openRow(title);
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(2000);
      if (!page.url().endsWith('/admin-next/images')) throw new Error('not back on list: ' + page.url());
      if (await findRow(title)) throw new Error('still listed');
    });
  }

  /* DOCUMENTS */

  const docTitle = `Smoke-doc-${stamp}`;
  await c.step('documents list renders', async () => {
    await page.goto('/admin-next/documents'); await c.settle();
    await c.shot('doc-1-list');
  });

  await c.step('upload document', async () => {
    await page.click('.page-actions a:has-text("Upload Document")'); await c.settle();
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    const msgs = (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
    if (!/File/.test(msgs)) throw new Error('no file validation: ' + msgs);
    await page.locator('.form-field-image-upload input[type=file]').setInputFiles({ name: `smoke-doc-${stamp}.txt`, mimeType: 'text/plain', buffer: Buffer.from('hello from the blazor admin smoke test\n'.repeat(50)) });
    await page.waitForTimeout(1200);
    const title = await c.field('.page-body', 'Title:').inputValue();
    if (title !== docTitle) throw new Error('title was ' + title);
    await c.field('.page-body', 'Description:').fill('A smoke test document'); await c.field('.page-body', 'Description:').blur();
    await c.shot('doc-2-chosen');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const msg = await text('.page-body');
    if (!page.url().endsWith('/admin-next/documents')) throw new Error('not back on list: ' + page.url() + ' ' + msg.slice(0, 300));
    const row = await findRow(docTitle);
    console.log('       ' + row);
    if (!row || !row.toLowerCase().includes(`smoke-doc-${stamp}.txt`)) throw new Error('row missing');
    await c.shot('doc-3-list');
  });

  await c.step('document download link works', async () => {
    const href = await page.locator(`table.table tr:has-text("${docTitle}") td:nth-child(2) a`).getAttribute('href');
    const res = await page.request.get(href);
    console.log(`       ${href} -> ${res.status()}`);
    if (res.status() !== 200 || !(await res.text()).includes('hello from the blazor admin')) throw new Error('download failed');
  });

  await c.step('filter by extension', async () => {
    await page.click('a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await page.locator('.search-filter').getByLabel('Extension:', { exact: true }).selectOption('txt');
    await c.btn('.search-filter', 'Apply').click(); await c.settle();
    const rows = await c.rows();
    if (!rows.some(r => r.includes(docTitle))) throw new Error('doc missing after filter');
    if (rows.some(r => !/\.txt/.test(r))) throw new Error('non-txt rows: ' + rows.join(' | '));
  });

  await c.step('document details: edit description', async () => {
    await page.goto('/admin-next/documents'); await c.settle();
    await openRow(docTitle);
    await c.shot('doc-4-details');
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await c.field('.page-body', 'Description:').fill('Edited description'); await c.field('.page-body', 'Description:').blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const body = await text('.page-body');
    if (!/saved successfully/.test(body) || !/Edited description/.test(body)) throw new Error(body.slice(0, 400));
  });

  await c.step('delete document', async () => {
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/documents')) throw new Error('not back on list: ' + page.url());
    if (await findRow(docTitle)) throw new Error('still listed');
  });
};
