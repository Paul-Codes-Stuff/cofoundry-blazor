// Server-side validation errors shown against their fields (FormStatus + AdminFormFields), through a
// duplicate directory url path: the domain's UniqueConstraintViolationException names "UrlPath", so the
// message must show under the Url Path field (not in the form status list), and go when the field changes.
// Deletes the directory it creates.
module.exports = async (page, c) => {
  const dirPath = `srvval-${Date.now().toString(36)}`;
  const urlPath = () => page.locator('.page-body .control-group:has(label:text-is("Url Path:"))');
  const status = () => page.locator('.page-body .form-message.error');

  await c.step('create a directory', async () => {
    await page.goto('/admin-next/directories/new'); await c.settle();
    await c.field('.page-body', 'Url Path:').fill(dirPath); await c.field('.page-body', 'Url Path:').blur();
    await c.field('.page-body', 'Parent Directory:').selectOption({ label: '/' });
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    if (!/\/admin-next\/directories$/.test(page.url())) throw new Error('not saved');
  });

  await c.step('duplicate url path: error under the field', async () => {
    await page.goto('/admin-next/directories/new'); await c.settle();
    await c.field('.page-body', 'Url Path:').fill(dirPath); await c.field('.page-body', 'Url Path:').blur();
    await c.field('.page-body', 'Parent Directory:').selectOption({ label: '/' });
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(1500);
    if (!/\/directories\/new$/.test(page.url())) throw new Error('saved a duplicate');
    const fieldText = (await urlPath().locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
    if (!fieldText.includes(dirPath) && !/already/i.test(fieldText)) throw new Error('no message under the field: ' + fieldText);
    if (!(await urlPath().locator('.control-group-area.invalid').count())) throw new Error('field not marked invalid');
    if (!(await status().count())) throw new Error('no form status heading');
    if (await status().locator('li').count()) throw new Error('error also listed in the status: ' + await status().innerText());
    console.log('[field message] ' + fieldText);
    await c.shot('server-validation-1');
  });

  await c.step('changing the field clears its server error', async () => {
    await c.field('.page-body', 'Url Path:').fill(`${dirPath}-b`); await c.field('.page-body', 'Url Path:').blur();
    await page.waitForTimeout(500);
    const left = (await urlPath().locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ').trim();
    if (left) throw new Error('message still shown: ' + left);
  });

  await c.step('cleanup', async () => {
    await page.goto('/admin-next/directories'); await c.settle();
    await page.click(`table.table a:text-is("/${dirPath}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if ((await c.rows()).some(r => r.includes(dirPath))) throw new Error('directory still listed');
  });
};
