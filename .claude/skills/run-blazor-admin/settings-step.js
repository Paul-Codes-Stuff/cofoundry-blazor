// Site Settings: view, cancel, validation, save (checked via /robots.txt), clear cache. Restores the
// original values at the end so the site is left as it was.
module.exports = async (page, c) => {
  const marker = `# blazor-admin-smoke ${Date.now().toString(36)}`;
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const robots = () => c.field('.page-body', 'Robots.txt:');
  const appName = () => c.field('.page-body', 'Application Name:');
  let original;

  await c.step('settings page renders read-only', async () => {
    await page.goto('/admin-next/settings'); await c.settle();
    const text = await body();
    if (!/General/.test(text) || !/Seo/i.test(text)) throw new Error(text.slice(0, 300));
    if (await page.locator('.page-body textarea').count()) throw new Error('inputs shown before Edit');
    await c.shot('settings-1-view');
  });

  await c.step('Help toggles field descriptions', async () => {
    const help = page.locator('fieldset:has(legend:has-text("Seo")) .toggle-helpers');
    const desc = page.locator('text=This file gives instructions about this site');
    if (!(await help.isVisible())) throw new Error('Help button hidden');
    if (await desc.isVisible()) throw new Error('description visible before Help clicked');
    await help.click(); await page.waitForTimeout(500);
    if (!(await desc.isVisible())) throw new Error('description not shown after Help clicked');
    await c.shot('settings-help');
    await help.click(); await page.waitForTimeout(500);
  });

  await c.step('edit then cancel discards changes', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    original = { robots: await robots().inputValue(), appName: await appName().inputValue() };
    await robots().fill('should be discarded'); await robots().blur();
    await c.btn('.page-actions', 'Cancel').click(); await page.waitForTimeout(800);
    if (/should be discarded/.test(await body())) throw new Error('cancel kept the edit');
  });

  await c.step('application name is required', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await appName().fill(''); await appName().blur();
    try {
      await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
      // The message uses the command's property name, as the domain has no [Display] name for it.
      const msgs = (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
      // Messages use the field's label, not the property name.
      if (!/The Application Name field is required/.test(msgs)) throw new Error('no validation: ' + msgs);
      await c.shot('settings-2-validation');
    } finally {
      await appName().fill(original.appName); await appName().blur();
    }
  });

  await c.step('save updates robots.txt', async () => {
    await robots().fill(original.robots + '\n' + marker); await robots().blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const text = await body();
    if (!/saved successfully/.test(text) || !text.includes(marker)) throw new Error(text.slice(0, 400));
    const served = await (await page.request.get('/robots.txt')).text();
    if (!served.includes(marker)) throw new Error('/robots.txt not updated: ' + served.slice(0, 200));
    await c.shot('settings-3-saved');
  });

  await c.step('clear cache', async () => {
    await c.btn('.page-actions', 'Clear Cache').click(); await c.settle(1500);
    if (!/Cache cleared/.test(await body())) throw new Error('no message');
  });

  await c.step('restore original settings', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await robots().fill(original.robots); await robots().blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if ((await body()).includes(marker)) throw new Error('marker still present');
  });
};
