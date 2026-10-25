// OpenGraph image picker on a page: select + save, reopen shows current selection, remove, double-click
// select, upload from inside the picker. Uses an existing "smoke-" page (run `smoke` first if there is
// none) and deletes the images it uploads.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const pickTitle = `Smoke-og-${stamp}`;
  const uploadTitle = `Smoke-ogup-${stamp}`;
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const field = () => page.locator('.control-group:has(label:text-is("OpenGraph Image:"))');
  const dialog = () => page.locator('.modal:has(.modal-title:text-is("Find an image"))');
  const previewTitle = async () => (await field().locator('img').count()) ? field().locator('img').getAttribute('title') : null;
  let pageUrl;

  await c.step('setup: upload an image to pick', async () => {
    await page.goto('/admin-next/images/new'); await c.settle();
    await page.locator('.form-field-image-upload input[type=file]').setInputFiles({ name: `smoke-og-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(600, 400, '#a5f') });
    await page.waitForTimeout(1500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!page.url().endsWith('/admin-next/images')) throw new Error('upload failed');
  });

  await c.step('create page form has the image field', async () => {
    await page.goto('/admin-next/pages/new'); await c.settle();
    if (!(await field().locator('button[title="Select"]').count())) throw new Error('no Select button on create form');
  });

  await c.step('pick image on a page and save', async () => {
    await page.goto('/admin-next/pages'); await c.settle();
    await page.click('table.table a:has-text("smoke-") >> nth=0'); await c.settle();
    pageUrl = page.url();
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    if (await previewTitle()) { await field().locator('button[title="Remove"]').click(); await page.waitForTimeout(500); }
    await field().locator('button[title="Select"]').first().click(); await page.waitForTimeout(1500);
    if (!(await dialog().count())) throw new Error('picker did not open');
    if (!(await dialog().locator('input[type=button][value="Select"], button:text-is("Select")').first().isDisabled())) throw new Error('Select enabled with nothing chosen');
    await dialog().locator(`tr:has-text("${pickTitle}")`).click(); await page.waitForTimeout(500);
    if (!(await dialog().locator(`tr.selected:has-text("${pickTitle}")`).count())) throw new Error('row not marked selected');
    await c.shot('picker-1-dialog');
    await dialog().locator('.page-actions button:text-is("Select")').click(); await page.waitForTimeout(1000);
    if (await dialog().count()) throw new Error('picker still open');
    if (await previewTitle() !== pickTitle) throw new Error('preview is ' + await previewTitle());
    await c.shot('picker-2-field');
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    await page.goto(pageUrl); await c.settle();
    if (await previewTitle() !== pickTitle) throw new Error('not saved, preview is ' + await previewTitle());
    await c.shot('picker-3-saved');
  });

  await c.step('reopening shows the current image selected; cancel keeps it', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await field().locator('button[title="Change"]').click(); await page.waitForTimeout(1500);
    if (!(await dialog().locator(`tr.selected:has-text("${pickTitle}")`).count())) throw new Error('current image not selected');
    await dialog().locator('.page-actions button:text-is("Cancel")').click(); await page.waitForTimeout(800);
    if (await previewTitle() !== pickTitle) throw new Error('cancel changed the image');
  });

  await c.step('upload from inside the picker selects the new image', async () => {
    await field().locator('button[title="Change"]').click(); await page.waitForTimeout(1500);
    await dialog().locator('a[title="Upload"]').click(); await page.waitForTimeout(1200);
    const up = page.locator('.modal:has(.modal-title:text-is("Upload an image"))');
    if (!(await up.count())) throw new Error('upload dialog did not open');
    await up.locator('input[type=file]').setInputFiles({ name: `smoke-ogup-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(300, 300, '#5a5') });
    await page.waitForTimeout(1500);
    await c.shot('picker-4-upload');
    await up.locator('input[type=submit][value="Upload"]').click(); await c.settle(2000);
    if (await page.locator('.modal').count()) throw new Error('a dialog is still open');
    if (await previewTitle() !== uploadTitle) throw new Error('preview is ' + await previewTitle());
  });

  await c.step('double-click selects and closes', async () => {
    await field().locator('button[title="Change"]').click(); await page.waitForTimeout(1500);
    await dialog().locator(`tr:has-text("${pickTitle}")`).dblclick(); await page.waitForTimeout(1000);
    if (await dialog().count()) throw new Error('picker still open');
    if (await previewTitle() !== pickTitle) throw new Error('preview is ' + await previewTitle());
  });

  await c.step('remove image and save', async () => {
    await field().locator('button[title="Remove"]').click(); await page.waitForTimeout(500);
    if (await previewTitle()) throw new Error('still has preview');
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    await page.goto(pageUrl); await c.settle();
    if (await previewTitle()) throw new Error('image not removed after save');
  });

  for (const title of [pickTitle, uploadTitle]) {
    await c.step(`cleanup: delete ${title}`, async () => {
      await page.goto('/admin-next/images'); await c.settle();
      await page.click(`table.table a:text-is("${title}")`); await c.settle();
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(2000);
      if (!page.url().endsWith('/admin-next/images')) throw new Error((await page.locator('.page-body').innerText()).slice(0, 300));
    });
  }
};
