// Custom entities (phase 1: simple fields and image fields) on the sandbox's Authors (SIMAUT),
// Categories (SIMCAT) and Blog Posts (SIMBLP). Pickers, drafts, versions and publishing are in
// custom-entity-pickers-step.js, nested data models (Examples) in nested-data-models-step.js and the other
// field types in field-types-step.js.
// Uploads an image, creates authors and a category, and deletes them all again.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const authorName = `CE Author ${stamp}`;
  const categoryName = `CE Category ${stamp}`;
  const imageTitle = `Ce-thumb-${stamp}`;
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  // Display names come from MVC metadata, so casing varies ("Short description", "Thumbnail Image").
  const control = label => page.locator(`.page-body .control-group:has(label:text-matches("^${label}$", "i"))`);
  const messages = async () => (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const deleteEntity = async (listPath, title) => {
    await page.goto(listPath); await c.settle();
    await page.click(`table.table a:text-is("${title}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith(listPath)) throw new Error('not back on the list: ' + (await body()).slice(0, 300));
    if ((await c.rows()).some(r => r.includes(title))) throw new Error(`${title} still listed`);
  };
  let authorUrl;

  await c.step('nav links custom entities to the Blazor module', async () => {
    await page.goto('/admin-next/dashboard'); await c.settle();
    const href = await page.locator('.main-nav a:text-is("Authors")').getAttribute('href');
    if (href !== '/admin-next/custom-entities/SIMAUT') throw new Error('Authors nav link is ' + href);
  });

  await c.step('authors list uses the custom title term', async () => {
    await page.goto('/admin-next/custom-entities/SIMAUT'); await c.settle();
    const headers = await page.locator('table.table thead th').allInnerTexts();
    if (!headers.map(h => h.trim().toUpperCase()).includes('NAME')) throw new Error('headers: ' + headers.join(' | '));
    if (headers.some(h => /status/i.test(h))) throw new Error('auto-publish entity shows a status column');
    await c.shot('ce-1-authors');
  });

  await c.step('create author validates the title', async () => {
    await page.goto('/admin-next/custom-entities/SIMAUT/new'); await c.settle();
    if (await page.locator('.page-body label:text-is("Url Slug:")').count()) throw new Error('slug field shown for an auto-generated slug');
    if (await c.btn('.page-actions', 'Save Draft').count()) throw new Error('Save Draft shown for an auto-publish entity');
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    if (!/Title/i.test(await messages())) throw new Error('no title validation: ' + await messages());
  });

  await c.step('create author with dynamic fields', async () => {
    await c.field('.page-body', 'Name:').fill(authorName); await c.field('.page-body', 'Name:').blur();
    const bio = control('Biography:').locator('textarea');
    if (await bio.getAttribute('maxlength') !== '500') throw new Error('biography maxlength not mapped');
    await bio.fill('Writes things.'); await bio.blur();
    if (!(await control('Profile Image:').locator('button[title="Select"]').count())) throw new Error('no image picker field');
    await c.shot('ce-2-author-new');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!/\/admin-next\/custom-entities\/SIMAUT\/\d+$/.test(page.url())) throw new Error('not on details: ' + (await body()).slice(0, 300));
    authorUrl = new URL(page.url()).pathname;
    const text = await body();
    if (!text.includes('Writes things.') || /Versions/.test(text)) throw new Error(text.slice(0, 400));
    await c.shot('ce-3-author-details');
  });

  await c.step('edit author data model', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    const bio = control('Biography:').locator('textarea');
    await bio.fill('Writes other things.'); await bio.blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    let text = await body();
    if (!/Changes were saved successfully/.test(text) || !text.includes('Writes other things.')) throw new Error(text.slice(0, 400));
    await page.goto(authorUrl); await c.settle();
    text = await body();
    if (!text.includes('Writes other things.')) throw new Error('not persisted: ' + text.slice(0, 300));
  });

  await c.step('cancel discards data model changes', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    const bio = control('Biography:').locator('textarea');
    await bio.fill('Unsaved change'); await bio.blur();
    await c.btn('.page-actions', 'Cancel').click(); await c.settle(1000);
    const text = await body();
    if (text.includes('Unsaved change') || !text.includes('Writes other things.')) throw new Error(text.slice(0, 300));
  });

  await c.step('duplicate author', async () => {
    await c.btn('.page-actions', 'Duplicate').click(); await page.waitForTimeout(1000);
    const title = await c.field('.modal', 'Name:').inputValue();
    if (title !== `Copy of ${authorName}`) throw new Error('duplicate title ' + title);
    await c.btn('.modal', 'Create duplicate').click(); await c.settle(2000);
    if (new URL(page.url()).pathname === authorUrl || !/\/SIMAUT\/\d+$/.test(page.url())) throw new Error('not on the copy: ' + page.url());
    if (!(await body()).includes('Writes other things.')) throw new Error('copy lost the data model');
  });

  await c.step('blog post validates data model annotations', async () => {
    await page.goto('/admin-next/custom-entities/SIMBLP/new'); await c.settle();
    if (!(await c.btn('.page-actions', 'Save Draft').count())) throw new Error('no Save Draft for a non auto-publish entity');
    if (!(await page.locator('.page-body label:text-is("Url Slug:")').count())) throw new Error('no slug field');
    if (!(await control('Author:').locator('input.ui-select-search').count())) throw new Error('no author selector');
    await c.field('.page-body', 'Title:').fill(`CE Post ${stamp}`); await c.field('.page-body', 'Title:').blur();
    await page.waitForTimeout(400);
    if (await c.field('.page-body', 'Url Slug:').inputValue() !== `ce-post-${stamp}`) throw new Error('slug not generated');
    await c.btn('.page-actions', 'Save Draft').click(); await page.waitForTimeout(800);
    const msgs = await messages();
    // [Required] on ShortDescription, and the schema's IsRequired on the non-nullable thumbnail id.
    if (!/Short ?Description field is required/i.test(msgs) || !/Thumbnail Image field is required/i.test(msgs)) throw new Error('messages: ' + msgs);
    const desc = control('Short Description:').locator('textarea');
    await desc.fill('Something'); await desc.blur(); await page.waitForTimeout(400);
    if (/Short ?Description field is required/i.test(await messages())) throw new Error('message not cleared on change');
    await c.shot('ce-4-blog-validation');
  });

  await c.step('setup: upload an image', async () => {
    await page.goto('/admin-next/images/new'); await c.settle();
    // At least 460x460: the sandbox's [Image(MinWidth = 460, MinHeight = 460)] fields filter smaller images out of the picker.
    await page.locator('.form-field-image-upload input[type=file]').setInputFiles({ name: `ce-thumb-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(500, 500, '#3a8') });
    await page.waitForTimeout(1500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error('upload failed');
  });

  await c.step('create category with preview image and description', async () => {
    await page.goto('/admin-next/custom-entities/SIMCAT/new'); await c.settle();
    await c.field('.page-body', 'Title:').fill(categoryName); await c.field('.page-body', 'Title:').blur();
    const desc = control('Short Description:').locator('textarea');
    await desc.fill(`About ${stamp}`); await desc.blur();
    await control('Thumbnail Image:').locator('button[title="Select"]').first().click(); await page.waitForTimeout(1500);
    const dialog = page.locator('.modal:has(.modal-title:text-is("Find an image"))');
    await dialog.locator(`tr:has-text("${imageTitle}")`).click(); await page.waitForTimeout(400);
    await dialog.locator('.page-actions button:text-is("Select")').click(); await page.waitForTimeout(1000);
    if (await control('Thumbnail Image:').locator('img').getAttribute('title') !== imageTitle) throw new Error('image not picked');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!/\/SIMCAT\/\d+$/.test(page.url())) throw new Error('not on details: ' + (await body()).slice(0, 300));
    if (await control('Thumbnail Image:').locator('img').getAttribute('title') !== imageTitle) throw new Error('image not saved');
  });

  await c.step('category list shows preview columns', async () => {
    await page.goto('/admin-next/custom-entities/SIMCAT'); await c.settle();
    const headers = (await page.locator('table.table thead th').allInnerTexts()).map(h => h.trim());
    if (!headers.some(h => /short description/i.test(h))) throw new Error('headers: ' + headers.join(' | '));
    const row = page.locator(`table.table tbody tr:has(a:text-is("${categoryName}"))`);
    if (!(await row.innerText()).includes(`About ${stamp}`)) throw new Error('no preview description');
    if (await row.locator('img').getAttribute('title') !== imageTitle) throw new Error('no preview image');
    await c.shot('ce-5-categories');
  });

  await c.step('filter by text', async () => {
    await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await c.field('.search-filter', 'Text:').fill(categoryName); await c.field('.search-filter', 'Text:').blur();
    await c.btn('.search-filter', 'Apply').click(); await c.settle(1000);
    const rows = await c.rows();
    if (rows.length !== 1 || !rows[0].includes(categoryName)) throw new Error(rows.join(' | '));
  });

  await c.step('cleanup: delete entities and image', async () => {
    await deleteEntity('/admin-next/custom-entities/SIMCAT', categoryName);
    await deleteEntity('/admin-next/custom-entities/SIMAUT', `Copy of ${authorName}`);
    await deleteEntity('/admin-next/custom-entities/SIMAUT', authorName);
    await page.goto('/admin-next/images'); await c.settle();
    await page.click(`table.table a:text-is("${imageTitle}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error((await body()).slice(0, 300));
  });
};
