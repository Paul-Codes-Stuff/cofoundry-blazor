// Unexpected exceptions in a page show a message (AdminLayout's error boundary) instead of freezing the
// page, and the rest of the admin keeps working. Uses the sandbox's dev-only page at
// /admin-next/test/error (Dev.Sandbox/Cofoundry/Admin/Blazor/Shared/ErrorBoundaryTestPage.razor).
// Each run logs two "Error boundary test" exceptions in the sandbox log; they are expected.
module.exports = async (page, c) => {
  const text = async () => (await page.locator('.content').innerText()).replace(/\s+/g, ' ').trim();

  await c.step('exception from an event handler', async () => {
    await page.goto('/admin-next/test/error'); await c.settle();
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

  await c.step('exception while a page loads', async () => {
    await page.goto('/admin-next/test/error?onload=true'); await c.settle();
    if (!/Sorry, an unexpected error has occurred/.test(await text())) throw new Error((await text()).slice(0, 300));
  });

  await c.step('navigating away recovers', async () => {
    await page.click('nav.main-nav a.main-button:has-text("Content")'); await c.settle();
    const t = await text();
    if (/unexpected error/.test(t) || /test\/error/.test(page.url())) throw new Error(page.url() + ' ' + t.slice(0, 300));
  });
};
