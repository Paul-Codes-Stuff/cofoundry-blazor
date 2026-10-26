// Optional upload fields have a "Remove" button once a file is chosen; required ones (every built-in form)
// don't. Uses the sandbox's dev-only page at /admin-next/test/upload-fields
// (Dev.Sandbox/Cofoundry/Admin/Blazor/Shared/UploadFieldsTestPage.razor), whose image field shows the first
// image in the library as its saved file, so it needs one. Saves nothing.
module.exports = async (page, c) => {
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const field = label => page.locator(`.control-group:has(label:text-is("${label}"))`);
  const remove = label => field(label).locator('button:text-is("Remove")');
  const values = async () => (await page.locator('#upload-fields-values').innerText()).trim();

  await c.step('required upload fields have no Remove', async () => {
    await page.goto('/admin-next/images/new'); await c.settle();
    await page.locator('.form-field-image-upload input[type=file]').setInputFiles({ name: 'required.png', mimeType: 'image/png', buffer: await makePng(200, 100, '#c33') });
    await page.waitForTimeout(1500);
    if (!(await page.locator('.form-field-image-upload img[alt="required.png"]').count())) throw new Error('file not chosen');
    if (await page.locator('.form-field-image-upload button:text-is("Remove")').count()) throw new Error('Remove on a required field');
  });

  await c.step('image: Remove clears the chosen file, back to the saved one', async () => {
    await page.goto('/admin-next/test/upload-fields'); await c.settle();
    const saved = await field('Image:').locator('img').first().getAttribute('alt');
    if (!saved || saved === 'chosen.png') throw new Error('no saved image shown (the library needs an image): ' + saved);
    if (await remove('Image:').count()) throw new Error('Remove shown for the saved image');
    await field('Image:').locator('input[type=file]').setInputFiles({ name: 'chosen.png', mimeType: 'image/png', buffer: await makePng(300, 200, '#36c') });
    await page.waitForTimeout(1500);
    if ((await values()) !== 'Image: chosen.png, Document: none') throw new Error(await values());
    await c.shot('upload-remove-1');
    // A real click: the button must sit above the transparent file input covering the drop area.
    await remove('Image:').click(); await page.waitForTimeout(800);
    if ((await values()) !== 'Image: none, Document: none') throw new Error(await values());
    if ((await field('Image:').locator('img').first().getAttribute('alt')) !== saved) throw new Error('saved image not shown again');
    if (await remove('Image:').count()) throw new Error('Remove still shown');
  });

  await c.step('document: Remove clears the chosen file', async () => {
    if (await remove('Document:').count()) throw new Error('Remove shown with no file');
    await field('Document:').locator('input[type=file]').setInputFiles({ name: 'chosen.txt', mimeType: 'text/plain', buffer: Buffer.from('upload remove test\n') });
    await page.waitForTimeout(1200);
    if ((await values()) !== 'Image: none, Document: chosen.txt') throw new Error(await values());
    if (!/chosen\.txt/.test(await field('Document:').innerText())) throw new Error('file name not shown');
    await remove('Document:').click(); await page.waitForTimeout(800);
    if ((await values()) !== 'Image: none, Document: none') throw new Error(await values());
    if (/chosen\.txt/.test(await field('Document:').innerText())) throw new Error('file name still shown');
    // The input is reset, so the same file can be chosen again.
    await field('Document:').locator('input[type=file]').setInputFiles({ name: 'chosen.txt', mimeType: 'text/plain', buffer: Buffer.from('upload remove test\n') });
    await page.waitForTimeout(1200);
    if ((await values()) !== 'Image: none, Document: chosen.txt') throw new Error('could not choose again: ' + await values());
  });
};
