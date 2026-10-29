// Unexpected exceptions in a page show a message (AdminLayout's error boundary) instead of freezing the
// page, and the rest of the admin keeps working; while a dialog's content renders they show in the dialog
// (ModalDialog's). All are added to the Error Logging plugin's log (IAdminBlazorErrorHandler). Uses the sandbox's
// dev-only page at /admin-next/test/error (Dev.Sandbox/Cofoundry/Admin/Blazor/Shared/ErrorBoundaryTestPage.razor).
// Each run logs four "Error boundary test" exceptions in the sandbox log and the error log; they are expected.
module.exports = async (page, c) => {
  const stamp = 'eb-' + Date.now().toString(36);
  const text = async () => (await page.locator('.content').innerText()).replace(/\s+/g, ' ').trim();

  await c.step('exception from an event handler', async () => {
    await page.goto(`/admin-next/test/error?stamp=${stamp}`); await c.settle();
    await c.btn('.page-actions', 'Throw').click(); await c.settle(800);
    const t = await text();
    if (!/Sorry, an unexpected error has occurred/.test(t)) throw new Error(t.slice(0, 300));
    // The sandbox runs in Development, where the developer exception is shown.
    if (!/Error boundary test/.test(t)) throw new Error('no exception details: ' + t.slice(0, 300));
    if (await page.locator('#blazor-error-ui').isVisible()) throw new Error('the circuit failed');
    await c.shot('error-boundary-1');
  });

  await c.step('try again recovers', async () => {
    await page.click('.content a:text-is("Try again")'); await c.settle(800);
    const t = await text();
    if (/unexpected error/.test(t) || !/Click "Throw"/.test(t)) throw new Error(t.slice(0, 300));
  });

  await c.step('exception from a dialog button shows in the page, and the dialog closes', async () => {
    await page.locator('.page-actions button:text-is("Throw in dialog")').click(); await c.settle(800);
    await page.click('.modal .main-cta'); await c.settle(800);
    const t = await text();
    if (!/Sorry, an unexpected error has occurred/.test(t) || !/Error boundary test \(dialog\)/.test(t)) throw new Error(t.slice(0, 300));
    if (await page.locator('.modal').count()) throw new Error('dialog still open');
    if (await page.locator('#blazor-error-ui').isVisible()) throw new Error('the circuit failed');
    await page.click('.content a:text-is("Try again")'); await c.settle(800);
  });

  await c.step('exception while a dialog renders shows in the dialog; the page is unchanged', async () => {
    await page.locator('.page-actions button:text-is("Throw in dialog content")').click(); await c.settle(800);
    const dialog = (await page.locator('.modal').innerText()).replace(/\s+/g, ' ').trim();
    if (!/Sorry, an unexpected error has occurred/.test(dialog) || !/Error boundary test \(dialog content\)/.test(dialog)) throw new Error('dialog: ' + dialog.slice(0, 300));
    if (await page.locator('#blazor-error-ui').isVisible()) throw new Error('the circuit failed');
    if (!/Click "Throw"/.test(await text())) throw new Error('page replaced: ' + (await text()).slice(0, 300));
    await c.shot('error-boundary-2-dialog');
    await c.btn('.modal .modal-actions', 'Close').click(); await page.waitForTimeout(800);
    if (await page.locator('.modal').count()) throw new Error('dialog still open');
    // The page still works.
    await page.locator('.page-actions button:text-is("Throw in dialog")').click(); await c.settle(800);
    if (!/Click OK/.test(await page.locator('.modal').innerText())) throw new Error('dialog did not open');
    await c.btn('.modal .modal-actions', 'Cancel').click(); await page.waitForTimeout(800);
  });

  await c.step('exception while a page loads', async () => {
    await page.goto(`/admin-next/test/error?onload=true&stamp=${stamp}`); await c.settle();
    if (!/Sorry, an unexpected error has occurred/.test(await text())) throw new Error((await text()).slice(0, 300));
  });

  await c.step('navigating away recovers', async () => {
    await page.click('nav.main-nav a.main-button:has-text("Content")'); await c.settle();
    const t = await text();
    if (/unexpected error/.test(t) || /test\/error/.test(page.url())) throw new Error(page.url() + ' ' + t.slice(0, 300));
  });

  await c.step('all four are in the error log', async () => {
    await page.goto('/admin-next/errors'); await c.settle();
    await c.btn('.page-actions', 'Filter').click(); await page.waitForTimeout(300);
    await c.field('.page-actions', 'Text:').fill(stamp); await c.field('.page-actions', 'Text:').blur();
    await c.btn('.page-actions', 'Apply').click(); await c.settle(800);
    const rows = await c.rows();
    console.log('       ' + JSON.stringify(rows.map(r => r.slice(0, 70))));
    for (const message of [`Error boundary test ${stamp}`, `Error boundary test (dialog) ${stamp}`, `Error boundary test (dialog content) ${stamp}`, `Error boundary test (on load) ${stamp}`]) {
      if (!rows.some(r => r.includes(message))) throw new Error('not logged: ' + message);
    }
  });
};
