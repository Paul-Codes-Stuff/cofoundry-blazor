// Roles and Users (Cofoundry admin area): create a role with permissions, edit them, create a user in
// that role, filter, reset password, deactivate (moving the user out of the role), then delete the
// user and the role. Everything it creates is deleted again; the deleted user stays in the database
// as a soft-deleted row, as in the AngularJS admin. Reset password emails go to the sandbox's mail
// drop folder (the default LocalDrop send mode), so nothing is actually sent.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const roleTitle = `rt-${stamp}`;
  const email = `rt-${stamp}@example.com`;
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const msgs = async () => (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
  // Pages read/update permission checkboxes; the id is "permission" + permission code + entity code.
  const pagesRead = () => page.locator('#permissionCOMREDCOFPGE');
  const pagesUpdate = () => page.locator('#permissionCOMUPDCOFPGE');
  const filter = async fields => {
    await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    for (const [label, value] of Object.entries(fields)) {
      const f = c.field('.search-filter', label);
      if (typeof value === 'string' && (await f.evaluate(e => e.tagName)) === 'SELECT') await f.selectOption({ label: value });
      else { await f.fill(value); await f.blur(); }
    }
    await c.btn('.search-filter', 'Apply').click(); await c.settle(1000);
  };
  let roleUrl, userUrl;

  await c.step('roles list renders', async () => {
    await page.goto('/admin-next/roles'); await c.settle();
    const rows = await c.rows();
    if (!rows.some(r => /Super Administrator/.test(r))) throw new Error(rows.slice(0, 5).join(' | '));
    await c.shot('roles-1-list');
  });

  await c.step('create role validates title', async () => {
    await page.goto('/admin-next/roles/new'); await c.settle();
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    if (!/Title field is required/.test(await msgs())) throw new Error('no validation: ' + await msgs());
  });

  await c.step('create role with permissions', async () => {
    await c.field('.page-body', 'Title:').fill(roleTitle); await c.field('.page-body', 'Title:').blur();
    const area = c.field('.page-body', 'User Area:');
    if (await area.count()) await area.selectOption({ label: 'Cofoundry' });
    if (!(await pagesUpdate().isDisabled())) throw new Error('update enabled before read ticked');
    await pagesRead().check(); await page.waitForTimeout(400);
    if (await pagesUpdate().isDisabled()) throw new Error('update still disabled after read ticked');
    await pagesUpdate().check(); await page.waitForTimeout(400);
    await c.shot('roles-2-create');
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    if (!/\/admin-next\/roles$/.test(page.url())) throw new Error('not back on list: ' + page.url());
    await filter({ 'Text:': roleTitle });
    const link = page.locator(`table.table a:text-is("${roleTitle}")`);
    if (!(await link.count())) throw new Error((await c.rows()).join(' | '));
    await link.click(); await c.settle();
    roleUrl = page.url();
    if (!(await pagesRead().isChecked()) || !(await pagesUpdate().isChecked())) throw new Error('permissions not saved');
    if (!(await pagesRead().isDisabled())) throw new Error('checkbox editable in view mode');
    await c.shot('roles-3-details');
  });

  await c.step('unticking read clears the group', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await pagesRead().uncheck(); await page.waitForTimeout(400);
    if (await pagesUpdate().isChecked()) throw new Error('update still ticked');
    // Leave read ticked so the role keeps one permission.
    await pagesRead().check(); await page.waitForTimeout(400);
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    if (!/saved successfully/.test(await body())) throw new Error((await body()).slice(0, 300));
    if (!(await pagesRead().isChecked()) || (await pagesUpdate().isChecked())) throw new Error('wrong permissions after save');
  });

  await c.step('users list renders', async () => {
    await page.goto('/admin-next/users/COF'); await c.settle();
    const header = await page.locator('.page-header').innerText();
    if (!/Users/.test(header)) throw new Error(header);
    if ((await c.rows()).length < 1) throw new Error('no rows');
    await c.shot('users-1-list');
  });

  await c.step('nav links to the Blazor users list', async () => {
    const hrefs = await page.locator('nav a, .nav a, a').evaluateAll(as => as.map(a => a.getAttribute('href')));
    if (!hrefs.some(h => h && /\/admin-next\/users\/COF$/.test(h))) throw new Error('no /admin-next/users/COF link');
    if (!hrefs.some(h => h && /\/admin-next\/roles$/.test(h))) throw new Error('no /admin-next/roles link');
  });

  await c.step('create user validates email', async () => {
    await page.goto('/admin-next/users/COF/new'); await c.settle();
    if (!/temporary password/.test(await body())) throw new Error('no temporary password warning');
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    if (!/Email field is required/.test(await msgs())) throw new Error('no validation: ' + await msgs());
    await c.shot('users-2-create-validation');
  });

  await c.step('create user', async () => {
    const display = c.field('.page-body', 'Display Name:');
    if (await display.count()) { await display.fill(`Role Test ${stamp}`); await display.blur(); }
    await c.field('.page-body', 'Email:').fill(email); await c.field('.page-body', 'Email:').blur();
    await c.field('.page-body', 'Role:').selectOption({ label: roleTitle });
    // Hashing the temporary password and writing the email takes a few seconds.
    await c.btn('.page-actions', 'Save').click();
    await page.waitForURL(/\/admin-next\/users\/COF$/, { timeout: 15000 }).catch(() => {});
    await c.settle();
    if (!/\/admin-next\/users\/COF$/.test(page.url())) throw new Error('not back on list: ' + page.url() + ' ' + (await body()).slice(0, 300));
  });

  await c.step('filter users by email', async () => {
    await filter({ 'Email:': email });
    const rows = await c.rows();
    if (rows.length !== 1 || !rows[0].includes(email) || !rows[0].includes(roleTitle) || !/Never/.test(rows[0])) throw new Error(rows.join(' | '));
    await c.shot('users-3-filtered');
    await page.locator(`table.table a:text-is("${email}")`).click(); await c.settle();
    userUrl = page.url();
    const text = await body();
    if (!/Account Verified/i.test(text) || !/Password last changed/i.test(text)) throw new Error(text.slice(0, 400));
    await c.shot('users-4-details');
  });

  await c.step('reset password', async () => {
    await c.btn('.page-actions', 'Reset Password').click(); await page.waitForTimeout(800);
    const dialog = await page.locator('.modal').innerText();
    if (!/sign the user out of all sessions/.test(dialog) || !/Do you want to continue/.test(dialog)) throw new Error(dialog);
    await c.shot('users-5-reset-confirm');
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!/Password reset, notification sent/.test(await body())) throw new Error((await body()).slice(0, 300));
  });

  await c.step('deactivate user', async () => {
    await page.goto(userUrl); await c.settle();
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await c.field('.page-body', 'Account Active:').uncheck();
    // A role can't be deleted while any user references it, even a deleted one, so move the
    // user to another role before it's deleted.
    const other = (await c.field('.page-body', 'Role:').locator('option').allInnerTexts()).find(t => t !== roleTitle && t !== 'Please choose');
    await c.field('.page-body', 'Role:').selectOption({ label: other });
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    if (!/saved successfully/.test(await body())) throw new Error((await body()).slice(0, 300));
    await page.goto('/admin-next/users/COF'); await c.settle();
    await filter({ 'Email:': email });
    if ((await c.rows()).some(r => r.includes(email))) throw new Error('deactivated user listed under Active');
    await filter({ 'Email:': email, 'Active:': 'Any' });
    const rows = await c.rows();
    if (!rows.some(r => r.includes(email) && /\(deactivated\)/.test(r))) throw new Error(rows.join(' | '));
    await page.goto(userUrl); await c.settle();
    if (await c.btn('.page-actions', 'Reset Password').count()) throw new Error('Reset Password shown for a deactivated user');
  });

  await c.step('delete user', async () => {
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!/\/admin-next\/users\/COF$/.test(page.url())) throw new Error('not back on list: ' + page.url());
    await filter({ 'Email:': email, 'Active:': 'Any' });
    if ((await c.rows()).some(r => r.includes(email))) throw new Error('user still listed');
  });

  await c.step('delete role', async () => {
    await page.goto(roleUrl); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!/\/admin-next\/roles$/.test(page.url())) throw new Error('not back on list: ' + page.url() + ' ' + (await body()).slice(0, 300));
    await filter({ 'Text:': roleTitle });
    if ((await c.rows()).some(r => r.includes(roleTitle))) throw new Error('role still listed');
  });
};
