// My Account: details, edit and change password, and editing yourself in the Users module. Creates a
// Cofoundry admin user, sets its password through the forced change, then signs in as it in a separate
// browser context so the admin's own account is never changed. Changing your own email or password
// updates your security stamp, so each save must re-issue the auth cookie: the steps check the cookie
// changed. The user is deleted at the end (a soft-deleted row remains).
const path = require('path');

const { readMail } = require('./mail');

module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  let email = `account-${stamp}@example.com`;
  const password = `Account-${stamp}-pw`;
  const newPassword = `Account-${stamp}-new`;

  const userContext = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin, viewport: { width: 1200, height: 900 } });
  const user = await userContext.newPage();
  user.on('pageerror', e => console.log('[user pageerror] ' + e.message));
  user.on('console', m => { if (m.type() === 'error') console.log('[user console] ' + m.text()); });
  user.on('response', r => { if (r.status() >= 400 && !r.url().endsWith('favicon.ico')) console.log(`[user http ${r.status()}] ${r.url()}`); });
  const outDir = process.env.OUT_DIR || path.join(require('os').tmpdir(), 'blazor-admin-run');
  const userShot = async name => { await user.screenshot({ path: path.join(outDir, name + '.png'), fullPage: true }); };
  const bodyText = async () => (await user.locator('body').innerText()).replace(/\s+/g, ' ').trim();
  const submit = () => Promise.all([user.waitForNavigation(), user.click('input[type=submit]')]);
  const field = label => user.locator('.page-body').getByLabel(label, { exact: true });
  const authCookie = async () => JSON.stringify((await userContext.cookies()).filter(k => /cof/i.test(k.name)).map(k => k.value));
  const signIn = async (pw) => {
    await user.goto('/admin-next/auth/login');
    await user.fill('#Username', email); await user.fill('#Password', pw);
    await submit();
  };

  // Run from the step below, which screenshots the user's page if it fails.
  const editSelfInUsers = async () => {
    // The sandbox has only a few admin users, so the user is on the first page.
    await user.goto('/admin-next/users/COF'); await user.waitForTimeout(2500);
    await user.locator(`table.table a:text-is("${email}")`).click(); await user.waitForTimeout(2000);
    await user.locator('.page-actions button:text-is("Edit")').click(); await user.waitForTimeout(600);
    // Your own email can't be changed here (it would need the cookie re-issued), only in My Account.
    if (await field('Email Address:').count()) throw new Error('your own email is editable in Users');
    if (!/This is your own account/.test(await bodyText())) throw new Error('no own account warning');
    const display = field('Display Name:');
    if (await display.count()) { await display.fill(`Account Test ${stamp} self`); await display.blur(); }
    await user.locator('.page-actions input[type=submit][value="Save"]').click(); await user.waitForTimeout(2500);
    const text = await bodyText();
    await userShot('account-5-users-self');
    if (/could not be saved|unhandled exception/i.test(text) || !/Edit/i.test(text)) throw new Error(text.slice(0, 400));
    await user.goto('/admin-next/account');
    if (/\/auth\//.test(user.url()) || !(await bodyText()).includes(email)) throw new Error('signed out after editing yourself: ' + user.url());
  };

  try {
    await c.step('create user and set its password', async () => {
      const since = Date.now() - 1000;
      await page.goto('/admin-next/users/COF/new'); await c.settle();
      const display = c.field('.page-body', 'Display Name:');
      if (await display.count()) { await display.fill(`Account Test ${stamp}`); await display.blur(); }
      await c.field('.page-body', 'Email:').fill(email); await c.field('.page-body', 'Email:').blur();
      const roles = (await c.field('.page-body', 'Role:').locator('option').allInnerTexts()).filter(t => t !== 'Please choose');
      await c.field('.page-body', 'Role:').selectOption({ label: roles.find(r => /Super Administrator/.test(r)) || roles[0] });
      await c.btn('.page-actions', 'Save').click();
      await page.waitForURL(/\/admin-next\/users\/COF$/, { timeout: 15000 });
      const tempPassword = (await readMail(email, /account has been created/, since)).match(/temporary password is: (\S+)/)?.[1];
      if (!tempPassword) throw new Error('no temporary password in the email');
      await signIn(tempPassword);
      await user.fill('#Username', email); await user.fill('#OldPassword', tempPassword);
      await user.fill('#NewPassword', password); await user.fill('#ConfirmNewPassword', password);
      await submit();
      await signIn(password);
      if (/\/auth\//.test(user.url())) throw new Error('sign in failed: ' + user.url());
    });

    await c.step('nav links to My Account', async () => {
      await user.goto('/admin-next/dashboard');
      await user.waitForTimeout(1500);
      const link = user.locator('nav.main-nav a:text-is("My Account")');
      const href = await link.getAttribute('href');
      if (href !== '/admin-next/account') throw new Error('My Account links to ' + href);
      // The nav is interactive and the account pages are static SSR: the router must load the page.
      await Promise.all([user.waitForURL(/\/admin-next\/account$/), link.evaluate(a => a.click())]);
      await user.waitForTimeout(1000);
      const text = await bodyText();
      if (!/My Profile/.test(text) || !text.includes(email) || !/Password Last Changed/i.test(text)) throw new Error(text.slice(0, 400));
      await userShot('account-1-details');
    });

    await c.step('edit details validates', async () => {
      await Promise.all([user.waitForNavigation(), user.click('.page-actions a:text-is("Edit")')]);
      if (!/\?edit=true$/.test(user.url())) throw new Error('edit went to ' + user.url());
      await field('Email:').fill('');
      await submit();
      const text = await bodyText();
      if (!/The Email field is required/.test(text)) throw new Error(text.slice(0, 400));
      await userShot('account-2-edit-errors');
    });

    await c.step('edit details', async () => {
      const before = await authCookie();
      email = `account-${stamp}-2@example.com`;
      const display = field('Display Name:');
      if (await display.count()) await display.fill(`Account Test ${stamp} edited`);
      await field('Email:').fill(email);
      await submit();
      const text = await bodyText();
      if (!/\?saved=details$/.test(user.url()) || !/Changes were saved successfully/.test(text) || !text.includes(email)) throw new Error(user.url() + ' ' + text.slice(0, 400));
      if (await authCookie() === before) throw new Error('the auth cookie was not re-issued');
      await userShot('account-3-saved');
    });

    await c.step('change password validates', async () => {
      await Promise.all([user.waitForNavigation(), user.click('.page-actions a:text-is("Change Password")')]);
      if (!/\/admin-next\/account\/change-password$/.test(user.url())) throw new Error('link went to ' + user.url());
      await submit();
      let text = await bodyText();
      if (!/The Current Password field is required/.test(text) || !/The New Password field is required/.test(text)) throw new Error(text.slice(0, 400));
      await field('Current Password:').fill(password);
      await field('New Password:').fill(newPassword); await field('Confirm new password:').fill(newPassword + 'x');
      await submit();
      if (!/Passwords must match/.test(await bodyText())) throw new Error((await bodyText()).slice(0, 400));
      await field('Current Password:').fill('not-the-password');
      await field('New Password:').fill(newPassword); await field('Confirm new password:').fill(newPassword);
      await submit();
      text = await bodyText();
      if (/saved=password/.test(user.url())) throw new Error('wrong current password accepted');
      console.log('       ' + text.replace(/.*Change Password Cancel/, '').slice(0, 160));
      await userShot('account-4-password-errors');
    });

    await c.step('change password', async () => {
      const before = await authCookie();
      await field('Current Password:').fill(password);
      await field('New Password:').fill(newPassword); await field('Confirm new password:').fill(newPassword);
      await submit();
      if (!/\?saved=password$/.test(user.url()) || !/Your password has been changed/.test(await bodyText())) throw new Error(user.url() + ' ' + (await bodyText()).slice(0, 400));
      if (await authCookie() === before) throw new Error('the auth cookie was not re-issued');
      await user.goto('/admin-next/auth/logout');
      await signIn(newPassword);
      if (/\/auth\//.test(user.url())) throw new Error('sign in with the new password failed: ' + user.url());
    });

    await c.step('edit yourself in Users', async () => {
      try { await editSelfInUsers(); } catch (e) { await userShot('fail-account-users-self'); throw e; }
    });
  } finally {
    await userContext.close();

    await c.step('delete user', async () => {
      await page.goto('/admin-next/users/COF'); await c.settle();
      await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
      await c.field('.search-filter', 'Email:').fill(`account-${stamp}`); await c.field('.search-filter', 'Email:').blur();
      await c.btn('.search-filter', 'Apply').click(); await c.settle(1000);
      await page.locator('table.table tbody a').first().click(); await c.settle();
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(2000);
      if (!/\/admin-next\/users\/COF$/.test(page.url())) throw new Error('not back on list: ' + page.url());
    });
  }
};
