// An open admin tab notices when its sign in has gone (js/session-check.js): signing out in another tab
// sends it to sign in when it is focused again. Uses a copy of the admin's cookies in a separate browser
// context, so the admin's own session in `page` is left alone.
module.exports = async (page, c) => {
  const origin = new URL(page.url()).origin;
  const context = await page.context().browser().newContext({ baseURL: origin, storageState: await page.context().storageState(), viewport: { width: 1200, height: 900 } });
  const tab = await context.newPage();
  const focus = () => tab.evaluate(() => window.dispatchEvent(new Event('focus')));

  try {
    await c.step('session check while signed in', async () => {
      await tab.goto('/admin-next/pages'); await tab.waitForTimeout(2500);
      const status = await tab.evaluate(() => fetch('/admin-next/auth/session').then(r => r.status));
      if (status !== 204) throw new Error('session check returned ' + status);
      await focus(); await tab.waitForTimeout(1500);
      if (!/\/admin-next\/pages$/.test(tab.url())) throw new Error('signed-in tab went to ' + tab.url());
    });

    await c.step('signing out in another tab', async () => {
      const other = await context.newPage();
      await other.goto('/admin-next/auth/logout');
      await other.close();
      // The check is throttled to one every 15 seconds.
      await tab.waitForTimeout(15500);
      await Promise.all([tab.waitForURL(/\/admin-next\/auth\/login/, { timeout: 15000 }), focus()]);
      if (!/ReturnUrl=%2Fadmin-next%2Fpages/.test(tab.url())) throw new Error('no return url: ' + tab.url());
    });
  } finally {
    await context.close();
  }
};
