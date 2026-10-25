// Nested data models (phase 3) on the sandbox's Example (SIMEXA): the Blocks collection (one type,
// MinItems 2, [PreviewImage] only, so there's no title column) and TestCollection (multi-type). Covers
// add/edit/cancel/remove in the item dialog, validation in the dialog and on the collection, the image
// picker opened from inside the item dialog (stacked dialogs), drag to reorder, saving and editing.
// Then duplicates the Example and reorders the two with Change Ordering. Deletes everything again.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const titles = [`Nested A ${stamp}`, `Nested B ${stamp}`];
  const imageTitle = `Nested-thumb-${stamp}`;
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const messages = async () => (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
  const control = (label, scope = '.page-body') => page.locator(`${scope} .control-group:has(> .control-group-area > label:text-matches("^${label}$", "i"))`).first();
  const modal = () => page.locator('.modal').last();
  const modalField = label => modal().locator(`.control-group:has(label:text-matches("^${label}$", "i")) input, .control-group:has(label:text-matches("^${label}$", "i")) textarea`).first();
  const modalBtn = text => modal().locator(`button:text-is("${text}"), input[type=submit][value="${text}"]`).first();
  const rowsOf = async label => (await control(label).locator('tbody tr').allInnerTexts()).map(s => s.replace(/\s+/g, ' ').trim());
  const blocks = 'Blocks:';
  const collection = 'Test ?collection:';
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const addMultiType = async (typeText, fill) => {
    await control(collection).locator('.collection-actions .menu').hover(); await page.waitForTimeout(300);
    await control(collection).locator(`.menu-options button:text-matches("^Add ${typeText}$", "i")`).click(); await page.waitForTimeout(1000);
    await fill(); await modalBtn('OK').click(); await page.waitForTimeout(800);
  };
  const deleteExample = async title => {
    const list = '/admin-next/custom-entities/SIMEXA';
    await page.goto(list); await c.settle();
    if (!(await page.locator(`table.table a:text-is("${title}")`).count())) return;
    await page.click(`table.table a:text-is("${title}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith(list)) throw new Error(`delete ${title} failed: ` + (await body()).slice(0, 300));
  };

  await c.step('setup: an image', async () => {
    await page.goto('/admin-next/images/new'); await c.settle();
    await page.locator('.form-field-image-upload input[type=file]').setInputFiles({ name: `nested-thumb-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(400, 300, '#36c') });
    await page.waitForTimeout(1500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error('upload failed');
  });

  await c.step('collection min items is validated', async () => {
    await page.goto('/admin-next/custom-entities/SIMEXA/new'); await c.settle();
    await c.field('.page-body', 'Title:').fill(titles[0]); await c.field('.page-body', 'Title:').blur();
    if ((await rowsOf(blocks)).join() !== 'None') throw new Error('blocks: ' + (await rowsOf(blocks)).join(' | '));
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(1000);
    if (!/Blocks must have at least 2 items/i.test(await messages())) throw new Error('messages: ' + await messages());
    if (!/\/SIMEXA\/new$/.test(page.url())) throw new Error('saved without blocks');
  });

  await c.step('add a block with an image picked from inside the dialog', async () => {
    await control(blocks).locator('.collection-actions a[title="Add"]').click(); await page.waitForTimeout(1000);
    if ((await modal().locator('.modal-title').innerText()).trim() !== 'Add Item') throw new Error('no add dialog');
    await modalBtn('OK').click(); await page.waitForTimeout(600);
    if (!/Title field is required/i.test(await modal().innerText())) throw new Error('dialog did not validate');
    await modalField('Title:').fill('Teaser one'); await modalField('Title:').blur();
    await modal().locator('.control-group:has(label:text-matches("^Image:$", "i")) button[title="Select"]').first().click(); await page.waitForTimeout(1500);
    if ((await page.locator('.modal').count()) < 2) throw new Error('picker did not stack on the item dialog');
    const picker = page.locator('.modal:has(.modal-title:text-is("Find an image"))');
    await picker.locator(`tr:has-text("${imageTitle}")`).click(); await page.waitForTimeout(400);
    await c.shot('nested-1-stacked-dialogs');
    await picker.locator('.page-actions button:text-is("Select")').click(); await page.waitForTimeout(1000);
    if ((await modal().locator('.modal-title').innerText()).trim() !== 'Add Item') throw new Error('item dialog lost');
    if (!(await modal().locator('img').count())) throw new Error('image not shown in the item dialog');
    await modalBtn('OK').click(); await page.waitForTimeout(1000);
    if (await page.locator('.modal').count()) throw new Error('dialog still open: ' + (await modal().innerText()).slice(0, 200));
    if ((await control(blocks).locator('tbody tr').count()) !== 1) throw new Error('no row');
    if (!(await control(blocks).locator('tbody tr img').count())) throw new Error('no preview image');
  });

  await c.step('add a second block; edit and cancel keeps the item', async () => {
    await control(blocks).locator('.collection-actions a[title="Add"]').click(); await page.waitForTimeout(1000);
    await modalField('Title:').fill('Teaser two'); await modalField('Title:').blur();
    await modalBtn('OK').click(); await page.waitForTimeout(800);
    if ((await control(blocks).locator('tbody tr').count()) !== 2) throw new Error('not two rows');
    if (/Blocks must have at least 2 items/i.test(await messages())) throw new Error('min items message not cleared');
    // Cancel discards changes
    await control(blocks).locator('tbody tr').nth(1).locator('button[title="Edit"]').click(); await page.waitForTimeout(1000);
    if ((await modal().locator('.modal-title').innerText()).trim() !== 'Edit Item') throw new Error('no edit dialog');
    await modalField('Title:').fill('Changed then cancelled'); await modalField('Title:').blur();
    await modalBtn('Cancel').click(); await page.waitForTimeout(600);
    await control(blocks).locator('tbody tr').nth(1).locator('button[title="Edit"]').click(); await page.waitForTimeout(1000);
    if (await modalField('Title:').inputValue() !== 'Teaser two') throw new Error('cancel kept the change');
    await modalField('Title:').fill('Teaser two edited'); await modalField('Title:').blur();
    await modalBtn('OK').click(); await page.waitForTimeout(800);
    await control(blocks).locator('tbody tr').nth(1).locator('button[title="Edit"]').click(); await page.waitForTimeout(1000);
    if (await modalField('Title:').inputValue() !== 'Teaser two edited') throw new Error('edit not kept');
    await modalBtn('Cancel').click(); await page.waitForTimeout(600);
  });

  await c.step('multi-type collection: add each type, drag to reorder', async () => {
    await addMultiType('Content Blocky block', async () => {
      await modalField('Title:').fill('Content title'); await modalField('Title:').blur();
      await modalField('Text:').fill('Content text'); await modalField('Text:').blur();
    });
    await addMultiType('Header block', async () => {
      await modalField('Text:').fill('Header text'); await modalField('Text:').blur();
    });
    let rows = await rowsOf(collection);
    if (rows.length !== 2 || !/Content title.*Content text.*Content Blocky block/i.test(rows[0]) || !/Header block/i.test(rows[1])) throw new Error(rows.join(' | '));
    const trs = control(collection).locator('tbody tr');
    await trs.nth(1).dragTo(trs.nth(0)); await page.waitForTimeout(800);
    rows = await rowsOf(collection);
    if (!/Header block/i.test(rows[0])) throw new Error('not reordered: ' + rows.join(' | '));
    await c.shot('nested-2-form');
  });

  await c.step('save and view', async () => {
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!/\/SIMEXA\/\d+$/.test(page.url())) throw new Error('not saved: ' + (await messages()) + ' ' + (await body()).slice(0, 400));
    if ((await control(blocks).locator('tbody tr').count()) !== 2) throw new Error('blocks not saved');
    if (await control(blocks).locator('button[title="Edit"]').count()) throw new Error('actions shown in view mode');
    const rows = await rowsOf(collection);
    if (rows.length !== 2 || !/Header block/i.test(rows[0]) || !/Content title/.test(rows[1])) throw new Error('collection: ' + rows.join(' | '));
    await c.shot('nested-3-details');
  });

  await c.step('edit: removing a block fails min items, cancel restores', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await control(blocks).locator('tbody tr').first().locator('button[title="Remove"]').click(); await page.waitForTimeout(600);
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(1000);
    if (!/Blocks must have at least 2 items/i.test(await messages())) throw new Error('messages: ' + await messages());
    await c.btn('.page-actions', 'Cancel').click(); await page.waitForTimeout(800);
    if ((await control(blocks).locator('tbody tr').count()) !== 2) throw new Error('cancel did not restore');
  });

  await c.step('edit a multi-type item and save', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await control(collection).locator('tbody tr').nth(1).locator('button[title="Edit"]').click(); await page.waitForTimeout(1000);
    await modalField('Text:').fill('Content text edited'); await modalField('Text:').blur();
    await modalBtn('OK').click(); await page.waitForTimeout(800);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!(await body()).includes('Content text edited')) throw new Error((await messages()) + ' ' + (await body()).slice(0, 400));
  });

  await c.step('duplicate, then change ordering', async () => {
    await c.btn('.page-actions', 'Duplicate').click(); await page.waitForTimeout(1000);
    await c.field('.modal', 'Title:').fill(titles[1]); await c.field('.modal', 'Title:').blur();
    await c.btn('.modal', 'Create duplicate').click(); await c.settle(2000);
    if ((await control(blocks).locator('tbody tr').count()) !== 2) throw new Error('copy lost the blocks');
    await page.goto('/admin-next/custom-entities/SIMEXA'); await c.settle();
    const listOrder = async () => (await c.rows()).map(r => titles.findIndex(t => r.includes(t))).filter(i => i >= 0);
    const before = await listOrder();
    if (before.length !== 2) throw new Error('list: ' + (await c.rows()).join(' | '));
    await c.btn('.page-actions', 'Change Ordering').click(); await c.settle(1500);
    const dialogTitles = async () => (await modal().locator('tbody tr td:first-child').allInnerTexts()).map(s => s.trim());
    const names = await dialogTitles();
    const last = Math.max(names.indexOf(titles[0]), names.indexOf(titles[1]));
    const rows = modal().locator('tbody tr');
    await rows.nth(last).dragTo(rows.nth(0)); await page.waitForTimeout(600);
    const moved = (await dialogTitles())[0];
    await c.shot('nested-4-ordering');
    await modalBtn('Save').click(); await c.settle(1500);
    const after = await listOrder();
    if (titles[after[0]] !== moved) throw new Error(`expected ${moved} first, list: ` + (await c.rows()).join(' | '));
    if (before[0] === after[0]) throw new Error('order unchanged');
  });

  await c.step('cleanup', async () => {
    for (const t of titles) await deleteExample(t);
    await page.goto('/admin-next/images'); await c.settle();
    await page.click(`table.table a:text-is("${imageTitle}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error((await body()).slice(0, 300));
  });
};
