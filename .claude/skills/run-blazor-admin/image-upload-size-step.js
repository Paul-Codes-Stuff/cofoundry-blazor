// Uploading from an image picker checks the field's size restrictions, as the AngularJS upload field did.
// Uses the sandbox blog post's [Image(MinWidth = 460, MinHeight = 460)] thumbnail. Saves nothing.
module.exports = async (page, c) => {
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const upload = () => page.locator('.modal:has(.modal-title:text-is("Upload an image"))');
  const fileInput = () => upload().locator('.form-field-image-upload input[type=file]');

  await c.step('too small an image is rejected', async () => {
    await page.goto('/admin-next/blog-posts/new'); await c.settle();
    await page.locator('.control-group:has(label:text-is("Thumbnail Image:")) button[title="Select"]').first().click(); await page.waitForTimeout(1500);
    await page.locator('.modal:has(.modal-title:text-is("Find an image")) a[title="Upload"]').click(); await page.waitForTimeout(1200);
    const text = await upload().innerText();
    if (!/restrictions: min-size 460x460/.test(text)) throw new Error('no restrictions warning: ' + text.slice(0, 300));
    await fileInput().setInputFiles({ name: 'too-small.png', mimeType: 'image/png', buffer: await makePng(100, 100, '#3a6') });
    await page.waitForTimeout(1500);
    const after = await upload().innerText();
    if (!/The image is 100x100 which does not meet the size requirements/.test(after)) throw new Error(after.slice(0, 400));
    await c.shot('image-upload-size-1');
  });

  await c.step('a large enough image is accepted', async () => {
    await fileInput().setInputFiles({ name: 'big-enough.png', mimeType: 'image/png', buffer: await makePng(500, 500, '#3a6') });
    await page.waitForTimeout(1500);
    const text = await upload().innerText();
    if (/does not meet the size requirements/.test(text)) throw new Error(text.slice(0, 400));
    if (await upload().locator('.form-field-image-upload img').count() === 0) throw new Error('no preview');
  });
};
