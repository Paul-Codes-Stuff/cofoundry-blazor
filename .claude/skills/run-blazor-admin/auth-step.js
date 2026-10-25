// Forced change password, forgot password and reset password. Creates a Cofoundry admin user (which
// gets a temporary password that must be changed), then in a separate signed-out browser context:
// signs in with the temporary password, changes it, requests a reset link, opens it and resets the
// password again. The emails are read from the sandbox's mail drop folder (the default LocalDrop send
// mode), so nothing is actually sent. The user is deleted at the end (a soft-deleted row remains).
const path = require('path');

const { readMail } = require('./mail');

module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const email = `auth-${stamp}@example.com`;
  const changedPassword = `Changed-${stamp}-pw`;
  const resetPassword = `Reset-${stamp}-pw`;
  let tempPassword, userUrl, recoveryUrl;

  // A signed-out browser for the user, so the admin's session in `page` is left alone.
  const userContext = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin, viewport: { width: 1200, height: 900 } });
  const user = await userContext.newPage();
  user.on('pageerror', e => console.log('[user pageerror] ' + e.message));
  user.on('response', r => { if (r.status() >= 400 && !r.url().endsWith('favicon.ico')) console.log(`[user http ${r.status()}] ${r.url()}`); });
  const userText = async () => (await user.locator('.login-container').innerText()).replace(/\s+/g, ' ').trim();
  const userShot = async name => { await user.screenshot({ path: path.join(process.env.OUT_DIR || path.join(require('os').tmpdir(), 'blazor-admin-run'), name + '.png'), fullPage: true }); };
  const submit = () => Promise.all([user.waitForNavigation(), user.click('input[type=submit]')]);
  const signIn = async password => {
    await user.goto('/admin-next/auth/login');
    await user.fill('#Username', email); await user.fill('#Password', password);
    await submit();
  };

  // The sandbox is set up, so the setup page only redirects. Its form can't be tested without an empty db.
  await c.step('setup redirects once set up', async () => {
    await page.goto('/admin-next/setup'); await c.settle();
    if (!/\/admin-next\/dashboard$/.test(page.url())) throw new Error('setup went to ' + page.url());
  });

  try {
    await c.step('create user with a temporary password', async () => {
      const since = Date.now() - 1000;
      await page.goto('/admin-next/users/COF/new'); await c.settle();
      const display = c.field('.page-body', 'Display Name:');
      if (await display.count()) { await display.fill(`Auth Test ${stamp}`); await display.blur(); }
      await c.field('.page-body', 'Email:').fill(email); await c.field('.page-body', 'Email:').blur();
      const roles = (await c.field('.page-body', 'Role:').locator('option').allInnerTexts()).filter(t => t !== 'Please choose');
      await c.field('.page-body', 'Role:').selectOption({ label: roles.find(r => /Super Administrator/.test(r)) || roles[0] });
      await c.btn('.page-actions', 'Save').click();
      await page.waitForURL(/\/admin-next\/users\/COF$/, { timeout: 15000 });
      const mail = await readMail(email, /account has been created/, since);
      tempPassword = mail.match(/temporary password is: (\S+)/)?.[1];
      if (!tempPassword) throw new Error('no temporary password in the email');
      // AdminBlazorMailTemplateBuilder points the sign-in link at the Blazor admin.
      if (!/Sign in to your account here: http:\/\/localhost:\d+\/admin-next\s/.test(mail)) throw new Error('sign in link: ' + mail.match(/Sign in to your account here: \S+/)?.[0]);
    });
    if (!tempPassword) return;

    await c.step('sign in requires a password change', async () => {
      await signIn(tempPassword);
      if (!/\/admin-next\/auth\/change-password/.test(user.url())) throw new Error('landed on ' + user.url());
      if (!/You must change your password/.test(await userText())) throw new Error(await userText());
      await userShot('auth-1-change-password');
    });

    await c.step('change password validates', async () => {
      await submit();
      const text = await userText();
      if (!/Email field is required/.test(text) || !/Current password field is required/.test(text)) throw new Error(text);
      await user.fill('#Username', email); await user.fill('#OldPassword', 'not-the-password');
      await user.fill('#NewPassword', changedPassword); await user.fill('#ConfirmNewPassword', changedPassword + 'x');
      await submit();
      if (!/Password does not match/.test(await userText())) throw new Error(await userText());
      await user.fill('#OldPassword', 'not-the-password');
      await user.fill('#NewPassword', changedPassword); await user.fill('#ConfirmNewPassword', changedPassword);
      await submit();
      const wrong = await userText();
      if (/has been updated/.test(wrong) || !/Change Password/.test(wrong)) throw new Error('wrong current password accepted: ' + wrong);
      console.log('       ' + wrong.replace(/.*administrator area\.|.*admin area\./, '').slice(0, 160));
      await userShot('auth-2-change-password-errors');
    });

    await c.step('change password', async () => {
      await user.fill('#Username', email); await user.fill('#OldPassword', tempPassword);
      await user.fill('#NewPassword', changedPassword); await user.fill('#ConfirmNewPassword', changedPassword);
      await submit();
      if (!/Your password has been updated/.test(await userText())) throw new Error(await userText());
      await Promise.all([user.waitForNavigation(), user.click('a:text("Click here to login")')]);
      if (!/\/admin-next\/auth\/login/.test(user.url())) throw new Error('login link went to ' + user.url());
      await signIn(changedPassword);
      if (/\/auth\//.test(user.url())) throw new Error('sign in with the new password failed: ' + user.url());
      await user.goto('/admin-next/auth/logout');
    });

    await c.step('forgot password', async () => {
      await user.goto('/admin-next/auth/login');
      await Promise.all([user.waitForNavigation(), user.click('#forgotPasswordLink')]);
      if (!/\/admin-next\/auth\/forgot-password$/.test(user.url())) throw new Error('link went to ' + user.url());
      await submit();
      if (!/Email field is required/.test(await userText())) throw new Error(await userText());
      const since = Date.now() - 1000;
      await user.fill('#Username', email);
      await submit();
      if (!/Instructions on how to reset your password have been sent/.test(await userText())) throw new Error(await userText());
      await userShot('auth-3-forgot-sent');
      const mail = await readMail(email, /reset|recover/i, since);
      recoveryUrl = mail.split('=== TEXT ===')[1]?.match(/https?:\/\/\S+\/reset-password\?\S+/)?.[0];
      if (!recoveryUrl || !/\/admin-next\/auth\/reset-password\?/.test(recoveryUrl)) throw new Error('recovery link: ' + recoveryUrl);
    });

    await c.step('reset link with a bad token', async () => {
      await user.goto('/admin-next/auth/reset-password?t=not-a-token');
      const text = await userText();
      if (!/Invalid Request/.test(text) || !/Try again/.test(text)) throw new Error(text);
      console.log('       ' + text.slice(0, 160));
      await userShot('auth-4-reset-invalid');
    });
    if (!recoveryUrl) return;

    await c.step('reset password', async () => {
      await user.goto(recoveryUrl);
      if (!/Please enter a new password/.test(await userText())) throw new Error(await userText());
      await userShot('auth-5-reset-form');
      await user.fill('#NewPassword', resetPassword); await user.fill('#ConfirmNewPassword', resetPassword + 'x');
      await submit();
      if (!/Password does not match/.test(await userText())) throw new Error(await userText());
      await user.fill('#NewPassword', resetPassword); await user.fill('#ConfirmNewPassword', resetPassword);
      await submit();
      if (!/Password Change Complete/.test(await userText())) throw new Error(await userText());
      await userShot('auth-6-reset-complete');
      await signIn(resetPassword);
      if (/\/auth\//.test(user.url())) throw new Error('sign in with the reset password failed: ' + user.url());
    });

    await c.step('reset link only works once', async () => {
      await user.goto('/admin-next/auth/logout');
      await user.goto(recoveryUrl);
      if (!/Invalid Request/.test(await userText())) throw new Error(await userText());
    });
  } finally {
    await userContext.close();

    await c.step('delete user', async () => {
      await page.goto('/admin-next/users/COF'); await c.settle();
      await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
      await c.field('.search-filter', 'Email:').fill(email); await c.field('.search-filter', 'Email:').blur();
      await c.btn('.search-filter', 'Apply').click(); await c.settle(1000);
      await page.locator(`table.table a:text-is("${email}")`).click(); await c.settle();
      userUrl = page.url();
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(2000);
      if (!/\/admin-next\/users\/COF$/.test(page.url())) throw new Error('not back on list: ' + page.url() + ' from ' + userUrl);
    });
  }
};
