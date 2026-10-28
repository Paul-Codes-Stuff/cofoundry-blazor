// An open admin tab notices when its sign in has gone (js/session-check.js): signing out in another tab
// sends it to sign in when it is focused again, or when it moves to another admin page (back to that page
// after signing in). Uses a copy of the admin's cookies in a separate browser
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

    await c.step('moving to another page after signing out elsewhere', async () => {
      const second = await page.context().browser().newContext({ baseURL: origin, storageState: await page.context().storageState(), viewport: { width: 1200, height: 900 } });
      try {
        const navTab = await second.newPage();
        await navTab.goto('/admin-next/pages'); await navTab.waitForTimeout(2500);
        const other = await second.newPage();
        await other.goto('/admin-next/auth/logout');
        await other.close();
        await navTab.waitForTimeout(15500);
        await Promise.all([navTab.waitForURL(/\/admin-next\/auth\/login/, { timeout: 15000 }), navTab.$eval('a[href$="/images"]', a => a.click())]);
        if (!/ReturnUrl=%2Fadmin-next%2Fimages/.test(navTab.url())) throw new Error('no return url: ' + navTab.url());
        await navTab.fill('#Username', process.env.ADMIN_USER);
        await navTab.fill('#Password', process.env.ADMIN_PASS);
        await Promise.all([navTab.waitForURL(/\/admin-next\/images$/, { timeout: 15000 }), navTab.click('input[type=submit][value="Sign In"]')]);
      } finally {
        await second.close();
      }
    });
  } finally {
    await context.close();
  }
};
