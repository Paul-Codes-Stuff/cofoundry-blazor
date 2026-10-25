// Custom entity pickers (phase 2) and the draft workflow they unblock. Creates two authors, two categories
// and an image, then a blog post using the Author selector (search + inline "Add") and the Categories
// collection (picker, drag to reorder, remove). Then publishes, edits a draft, discards it, publishes a second
// version, copies V1 to draft and unpublishes, opens Change Ordering, and deletes
// everything again.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const authors = [`Pick Author A ${stamp}`, `Pick Author B ${stamp}`];
  const inlineAuthor = `Pick Author Inline ${stamp}`;
  const categories = [`Pick Cat A ${stamp}`, `Pick Cat B ${stamp}`];
  const postTitle = `Pick Post ${stamp}`;
  const imageTitle = `Pick-thumb-${stamp}`;
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const control = label => page.locator(`.page-body .control-group:has(label:text-matches("^${label}$", "i"))`);
  const modal = () => page.locator('.modal').last();
  const categoryRows = async () => (await control('Categories:').locator('tbody tr').allInnerTexts()).map(s => s.replace(/\s+/g, ' ').trim());
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const createEntity = async (code, titleLabel, title) => {
    await page.goto(`/admin-next/custom-entities/${code}/new`); await c.settle();
    await c.field('.page-body', titleLabel).fill(title); await c.field('.page-body', titleLabel).blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!new RegExp(`/${code}/\\d+$`).test(page.url())) throw new Error(`${title} not saved: ` + (await body()).slice(0, 300));
  };
  const deleteEntity = async (code, title) => {
    const list = `/admin-next/custom-entities/${code}`;
    await page.goto(list); await c.settle();
    if (!(await page.locator(`table.table a:text-is("${title}")`).count())) return;
    await page.click(`table.table a:text-is("${title}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith(list)) throw new Error(`delete ${title} failed: ` + (await body()).slice(0, 300));
  };
  const confirm = async button => {
    await c.btn('.page-actions', button).click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
  };
  const versions = async () => (await page.locator('fieldset:has(legend:has-text("Versions")) tbody tr td:first-child').allInnerTexts()).map(s => s.trim());
  let postUrl;

  await c.step('setup: authors, categories and an image', async () => {
    for (const a of authors) await createEntity('SIMAUT', 'Name:', a);
    for (const cat of categories) await createEntity('SIMCAT', 'Title:', cat);
    await page.goto('/admin-next/images/new'); await c.settle();
    await page.locator('.form-field-image-upload input[type=file]').setInputFiles({ name: `pick-thumb-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(400, 300, '#c63') });
    await page.waitForTimeout(1500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error('upload failed');
  });

  await c.step('author selector searches as you type', async () => {
    await page.goto('/admin-next/custom-entities/SIMBLP/new'); await c.settle();
    const search = control('Author:').locator('input.ui-select-search');
    await search.click(); await search.type(`Author B ${stamp}`); await page.waitForTimeout(1200);
    const options = (await control('Author:').locator('.ui-select-choices-row').allInnerTexts()).map(s => s.trim());
    if (options.length !== 1 || options[0] !== authors[1]) throw new Error('options: ' + options.join(' | '));
    await c.shot('pick-1-selector-search');
    await control('Author:').locator('.ui-select-choices-row').first().dispatchEvent('mousedown'); await page.waitForTimeout(600);
    if ((await control('Author:').locator('.ui-select-match').innerText()).trim() !== authors[1]) throw new Error('not selected');
  });

  await c.step('selector Add creates and selects an author', async () => {
    await control('Author:').locator('button[title="Add"]').click(); await page.waitForTimeout(1200);
    if (!/Create Author/.test(await modal().locator('.modal-title').innerText())) throw new Error('no create dialog');
    await c.field('.modal', 'Name:').fill(inlineAuthor); await c.field('.modal', 'Name:').blur();
    await c.btn('.modal', 'Save').click(); await c.settle(1500);
    if ((await control('Author:').locator('.ui-select-match').innerText()).trim() !== inlineAuthor) throw new Error('new author not selected');
    // Back to author A, by keyboard this time.
    const search = control('Author:').locator('input.ui-select-search');
    await control('Author:').locator('.ui-select-match').click(); await search.type(`Author A ${stamp}`); await page.waitForTimeout(1200);
    await search.press('Enter'); await page.waitForTimeout(600);
    if ((await control('Author:').locator('.ui-select-match').innerText()).trim() !== authors[0]) throw new Error('Enter did not select');
    if (!/\/SIMBLP\/new$/.test(page.url())) throw new Error('Enter submitted the form');
  });

  await c.step('categories picker adds several', async () => {
    await control('Categories:').locator('a[title="Add"]').click(); await page.waitForTimeout(1500);
    if (!/Find Category/.test(await modal().locator('.modal-title').innerText())) throw new Error('no picker');
    for (const cat of categories) {
      await modal().locator(`tr:has(a:text-is("${cat}"))`).click({ position: { x: 5, y: 5 } }); await page.waitForTimeout(400);
    }
    if ((await modal().locator('tr.selected').count()) !== 2) throw new Error('two rows not selected');
    await c.shot('pick-2-picker');
    await c.btn('.modal', 'Ok').click(); await page.waitForTimeout(1500);
    const rows = await categoryRows();
    if (rows.length !== 2 || !rows[0].startsWith(categories[0]) || !rows[1].startsWith(categories[1])) throw new Error(rows.join(' | '));
  });

  await c.step('drag reorders an orderable collection', async () => {
    const rows = control('Categories:').locator('tbody tr');
    await rows.nth(1).dragTo(rows.nth(0)); await page.waitForTimeout(800);
    const order = await categoryRows();
    if (!order[0].startsWith(categories[1])) throw new Error('not reordered: ' + order.join(' | '));
  });

  await c.step('save blog post draft with pickers', async () => {
    await c.field('.page-body', 'Title:').fill(postTitle); await c.field('.page-body', 'Title:').blur();
    const desc = control('Short description:').locator('textarea');
    await desc.fill('Picked things.'); await desc.blur();
    await control('Thumbnail Image:').locator('button[title="Select"]').first().click(); await page.waitForTimeout(1500);
    const dialog = page.locator('.modal:has(.modal-title:text-is("Find an image"))');
    await dialog.locator(`tr:has-text("${imageTitle}")`).click(); await page.waitForTimeout(400);
    await dialog.locator('.page-actions button:text-is("Select")').click(); await page.waitForTimeout(1000);
    await c.shot('pick-3-blog-form');
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    if (!/\/SIMBLP\/\d+$/.test(page.url())) throw new Error('not saved: ' + (await body()).slice(0, 500));
    postUrl = new URL(page.url()).pathname;
    const text = await body();
    if (!text.includes(authors[0])) throw new Error('author not shown: ' + text.slice(0, 500));
    const rows = await categoryRows();
    if (!rows[0].startsWith(categories[1]) || !rows[1].startsWith(categories[0])) throw new Error('order not saved: ' + rows.join(' | '));
    if ((await versions()).join('|') !== 'Draft') throw new Error('versions: ' + (await versions()).join('|'));
    await c.shot('pick-4-blog-details');
  });

  await c.step('remove a category in edit mode', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await control('Categories:').locator('tbody tr').first().locator('button[title="Remove"]').click(); await page.waitForTimeout(600);
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    const rows = await categoryRows();
    if (rows.length !== 1 || !rows[0].startsWith(categories[0])) throw new Error(rows.join(' | '));
  });

  await c.step('publish', async () => {
    await confirm('Publish');
    const text = await body();
    if (!/Blog Post published successfully/.test(text) || !/PUBLISHED STATUS: Published/i.test(text)) throw new Error(text.slice(0, 400));
    if ((await versions()).join('|') !== 'V1 (Published)') throw new Error('versions: ' + (await versions()).join('|'));
  });

  await c.step('edit creates a pending draft; discard it', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    const desc = control('Short description:').locator('textarea');
    await desc.fill('Draft change.'); await desc.blur();
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    if ((await versions()).join('|') !== 'Draft|V1 (Published)') throw new Error('versions: ' + (await versions()).join('|'));
    await page.locator('fieldset:has(legend:has-text("Versions")) a[title="Discard"]').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    const text = await body();
    if (!/Draft discarded successfully/.test(text) || text.includes('Draft change.')) throw new Error(text.slice(0, 400));
  });

  await c.step('publish a second version, copy V1 to draft, unpublish', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    const desc = control('Short description:').locator('textarea');
    await desc.fill('Second version.'); await desc.blur();
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    await confirm('Publish');
    if ((await versions()).join('|') !== 'V2 (Published)|V1') throw new Error('versions: ' + (await versions()).join('|'));
    await page.locator('fieldset:has(legend:has-text("Versions")) a[title="Copy to draft"]').first().click(); await c.settle(1500);
    if (await page.locator('.modal .main-cta').count()) { await page.click('.modal .main-cta'); await c.settle(1500); }
    if (!/Draft created successfully/.test(await body())) throw new Error((await body()).slice(0, 300));
    if ((await versions()).join('|') !== 'Draft|V2 (Published)|V1') throw new Error('versions: ' + (await versions()).join('|'));
    await confirm('Unpublish');
    if (!/has been unpublished and reverted to draft state/.test(await body())) throw new Error((await body()).slice(0, 300));
  });

  // Only Example (SIMEXA) is orderable in the sandbox (Category declares Ordering but not
  // IOrderableCustomEntityDefinition), and Examples need nested blocks, which phase 3 adds. So this only
  // opens the dialog; drag-reordering itself is covered on the collection field above.
  await c.step('change ordering dialog opens', async () => {
    await page.goto('/admin-next/custom-entities/SIMCAT'); await c.settle();
    if (await page.locator('.page-actions button:has-text("Change Ordering")').count()) throw new Error('unordered type offers ordering');
    await page.goto('/admin-next/custom-entities/SIMEXA'); await c.settle();
    const listed = (await c.rows()).filter(r => !/no example could be found/i.test(r)).length;
    await c.btn('.page-actions', 'Change Ordering').click(); await c.settle(1500);
    if ((await modal().locator('.modal-title').innerText()).trim() !== 'Update Ordering') throw new Error('no dialog');
    const rows = (await modal().locator('tbody tr').allInnerTexts()).map(s => s.trim());
    if (listed === 0 && rows.join() !== 'None') throw new Error('rows: ' + rows.join(' | '));
    if (listed > 0 && rows.length !== listed) throw new Error(`dialog lists ${rows.length}, list has ${listed}`);
    await c.shot('pick-5-ordering');
    await c.btn('.modal', 'Cancel').click(); await page.waitForTimeout(800);
    if (await page.locator('.modal').count()) throw new Error('dialog did not close');
  });

  await c.step('cleanup', async () => {
    await deleteEntity('SIMBLP', postTitle);
    for (const cat of categories) await deleteEntity('SIMCAT', cat);
    for (const a of [...authors, inlineAuthor]) await deleteEntity('SIMAUT', a);
    await page.goto('/admin-next/images'); await c.settle();
    await page.click(`table.table a:text-is("${imageTitle}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error((await body()).slice(0, 300));
  });
};
