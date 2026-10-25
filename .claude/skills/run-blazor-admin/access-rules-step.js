// Directories and page/directory access rules. Needs a user area besides the Cofoundry admin one (the
// sandbox's "Member" area, MEM): without it the admin hides Access Control.
// Creates a directory with a child directory, a published page in the child and a Member role, adds
// directory and page access rules, checks a signed-out request then gets a 404, changes the directory
// url, and deletes everything again (deleting the directory deletes the child and the page).
const { request } = require('playwright-core');

module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const dirPath = `acc-${stamp}`;
  const roleTitle = `acc-${stamp}`;
  const pageTitle = `Access ${stamp}`;
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const modal = () => page.locator('.modal').last();
  const modalRows = async section => (await modal().locator(`fieldset:has(legend:has-text("${section}")) table.table tbody tr`).allInnerTexts())
    .map(s => s.replace(/\s+/g, ' ').trim());
  const anonymous = await request.newContext({ baseURL: 'http://localhost:5006' });
  const anonStatus = async path => (await anonymous.get(path, { maxRedirects: 0 })).status();
  const addRule = async (userArea, role) => {
    await modal().locator('a.btn-icon[title="Add"]').click(); await page.waitForTimeout(800);
    await c.field('.modal', 'User Area:').selectOption({ label: userArea }); await page.waitForTimeout(600);
    if (role) {
      // A search-as-you-type dropdown: the search goes to the domain, so only the matching role is listed.
      await c.field('.modal', 'Role:').type(role); await page.waitForTimeout(1200);
      const rows = modal().locator('.ui-select-choices-row');
      const texts = (await rows.allInnerTexts()).map(t => t.trim());
      if (texts.length !== 1 || texts[0] !== role) throw new Error('role search listed: ' + texts.join(' | '));
      await rows.first().dispatchEvent('mousedown'); await page.waitForTimeout(600);
      if ((await modal().locator('.ui-select-match').innerText()).trim() !== role) throw new Error('role not selected');
    }
    await c.btn('.modal', 'Add').click(); await page.waitForTimeout(800);
  };
  let dirUrl, pageUrl, publicPath, roleUrl;

  await c.step('directories list renders', async () => {
    await page.goto('/admin-next/directories'); await c.settle();
    const rows = await c.rows();
    if (!rows.length || !rows[0].startsWith('/')) throw new Error(rows.slice(0, 3).join(' | '));
    await c.shot('dirs-1-list');
  });

  await c.step('create directory validates url path', async () => {
    await page.goto('/admin-next/directories/new'); await c.settle();
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(800);
    const msgs = (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
    if (!/Url path/i.test(msgs) || !/Parent directory/i.test(msgs)) throw new Error('no validation: ' + msgs);
  });

  await c.step('create directory and child', async () => {
    await c.field('.page-body', 'Url Path:').fill(dirPath); await c.field('.page-body', 'Url Path:').blur();
    await c.field('.page-body', 'Parent Directory:').selectOption({ label: '/' });
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    if (!/\/admin-next\/directories$/.test(page.url())) throw new Error('not back on list: ' + (await body()).slice(0, 300));
    await page.goto('/admin-next/directories/new'); await c.settle();
    await c.field('.page-body', 'Url Path:').fill('child'); await c.field('.page-body', 'Url Path:').blur();
    await c.field('.page-body', 'Parent Directory:').selectOption({ label: `/${dirPath}` });
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    const rows = await c.rows();
    if (!rows.some(r => r.startsWith(`/${dirPath}/child`))) throw new Error(rows.join(' | '));
    await page.click(`table.table a:text-is("/${dirPath}")`); await c.settle();
    dirUrl = new URL(page.url()).pathname;
    const text = await body();
    if (!/CHILD DIRECTORIES: 1/i.test(text) || !text.includes(`/${dirPath}/child`)) throw new Error(text.slice(0, 400));
    await c.shot('dirs-2-details');
  });

  await c.step('edit directory name', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    await c.field('.page-body', 'Name:').fill(`Access test ${stamp}`); await c.field('.page-body', 'Name:').blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    const text = await body();
    if (!/saved successfully/.test(text) || !text.includes(`Access test ${stamp}`)) throw new Error(text.slice(0, 300));
  });

  await c.step('create a Member role', async () => {
    await page.goto('/admin-next/roles/new'); await c.settle();
    await c.field('.page-body', 'Title:').fill(roleTitle); await c.field('.page-body', 'Title:').blur();
    await c.field('.page-body', 'User Area:').selectOption({ label: 'Member' });
    await c.btn('.page-actions', 'Save').click(); await c.settle(1500);
    if (!/\/admin-next\/roles$/.test(page.url())) throw new Error((await body()).slice(0, 300));
  });

  await c.step('create a published page in the child directory', async () => {
    await page.goto('/admin-next/pages/new'); await c.settle();
    await c.field('.page-body', 'Title:').fill(pageTitle); await c.field('.page-body', 'Title:').blur();
    await c.field('.page-body', 'Page Template:').selectOption({ label: 'General' });
    await c.field('.page-body', 'Directory:').selectOption({ label: `/${dirPath}/child` });
    await page.waitForTimeout(400);
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    if (!/\/admin-next\/pages\/\d+$/.test(page.url())) throw new Error('not on details: ' + (await body()).slice(0, 300));
    pageUrl = new URL(page.url()).pathname;
    await c.btn('.page-actions', 'Publish').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    publicPath = `/${dirPath}/child/access-${stamp}`;
    const status = await anonStatus(publicPath);
    if (status !== 200) throw new Error(`${publicPath} returned ${status} before any rules`);
  });

  await c.step('directory access rules: add, dedupe, save', async () => {
    await page.goto(dirUrl); await c.settle();
    await c.btn('.page-actions', 'Access Control').click(); await c.settle(1000);
    if (!(await modalRows('Access Rules')).some(r => /No rules found/.test(r))) throw new Error('expected no rules');
    if (await modal().locator('legend:has-text("Violation Action")').count()) throw new Error('violation section shown without rules');
    await addRule('Member');
    await addRule('Member');   // a duplicate is ignored
    await addRule('Member', roleTitle);
    const rows = await modalRows('Access Rules');
    if (rows.length !== 2 || !rows.some(r => r.includes(roleTitle))) throw new Error(rows.join(' | '));
    // Error is the enum's 0 value: it must show as selected, not as an empty "None" option.
    const actions = await c.field('.modal', 'Default Action:').evaluate(s => [...s.options].map(o => (o.selected ? '*' : '') + o.text));
    if (actions.join('|') !== '*Error (403: Forbidden)|Not Found (404: Not Found)') throw new Error('default action options: ' + actions.join(' | '));
    await c.field('.modal', 'Default Action:').selectOption({ label: 'Not Found (404: Not Found)' });
    await c.shot('access-1-directory-editor');
    await c.btn('.modal', 'Save').click(); await c.settle(1500);
    if (!/Access rules updated successfully/.test(await body())) throw new Error((await body()).slice(0, 300));
  });

  await c.step('rules saved and enforced', async () => {
    await c.btn('.page-actions', 'Access Control').click(); await c.settle(1000);
    const rows = await modalRows('Access Rules');
    if (rows.length !== 2) throw new Error(rows.join(' | '));
    const action = await c.field('.modal', 'Default Action:').evaluate(s => s.options[s.selectedIndex].text);
    if (!/Not Found/.test(action)) throw new Error('default action is ' + action);
    await c.btn('.modal', 'Cancel').click(); await page.waitForTimeout(600);
    const status = await anonStatus(publicPath);
    if (status !== 404) throw new Error(`${publicPath} returned ${status} for a signed-out user`);
  });

  await c.step('page access rules show inherited rules', async () => {
    await page.goto(pageUrl); await c.settle();
    await c.btn('.page-actions', 'Access Control').click(); await c.settle(1000);
    const inherited = await modalRows('Inherited Rules');
    if (inherited.length !== 2 || !inherited.every(r => r.includes(`/${dirPath}`) && /Not Found/.test(r))) throw new Error(inherited.join(' | '));
    await addRule('Member', roleTitle);
    await c.shot('access-2-page-editor');
    await c.btn('.modal', 'Save').click(); await c.settle(1500);
    if (!/Access rules updated successfully/.test(await body())) throw new Error((await body()).slice(0, 300));
    await c.btn('.page-actions', 'Access Control').click(); await c.settle(1000);
    const rows = await modalRows('Access Rules');
    if (rows.length !== 1 || !rows[0].includes(roleTitle)) throw new Error(rows.join(' | '));
    await c.btn('.modal', 'Cancel').click(); await page.waitForTimeout(600);
  });

  await c.step('removing the last rule hides the violation options', async () => {
    await c.btn('.page-actions', 'Access Control').click(); await c.settle(1000);
    await modal().locator('button[title="Delete"], a[title="Delete"]').first().click(); await page.waitForTimeout(500);
    if (await modal().locator('legend:has-text("Violation Action")').count()) throw new Error('violation section still shown');
    await c.btn('.modal', 'Save').click(); await c.settle(1500);
    if (!/Access rules updated successfully/.test(await body())) throw new Error((await body()).slice(0, 300));
  });

  await c.step('change directory url', async () => {
    await page.goto(dirUrl); await c.settle();
    await c.btn('.page-actions', 'Change Url').click(); await page.waitForTimeout(800);
    if (!/This directory contains other pages or directories/.test(await modal().innerText())) throw new Error('no warning');
    await c.field('.modal', 'Url Path:').fill(`${dirPath}-moved`); await c.field('.modal', 'Url Path:').blur();
    await c.btn('.modal', 'Save').click(); await c.settle(1500);
    const text = await body();
    if (!/Url Changed/.test(text) || !text.includes(`/${dirPath}-moved/child`)) throw new Error(text.slice(0, 400));
  });

  await c.step('pages link filters by directory', async () => {
    await page.click(`.page-body a:text-is("0")`); await c.settle();
    if (!/pageDirectoryId=\d+/.test(page.url())) throw new Error(page.url());
    if ((await c.rows()).some(r => r.includes(pageTitle))) throw new Error('page in the child directory listed under the parent');
  });

  await c.step('delete directory', async () => {
    await page.goto(dirUrl); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!/\/admin-next\/directories$/.test(page.url())) throw new Error((await body()).slice(0, 300));
    if ((await c.rows()).some(r => r.includes(dirPath))) throw new Error('directory still listed');
  });

  await c.step('delete the Member role', async () => {
    await page.goto('/admin-next/roles'); await c.settle();
    await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await c.field('.search-filter', 'Text:').fill(roleTitle); await c.field('.search-filter', 'Text:').blur();
    await c.btn('.search-filter', 'Apply').click(); await c.settle(1000);
    await page.click(`table.table a:text-is("${roleTitle}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!/\/admin-next\/roles$/.test(page.url())) throw new Error((await body()).slice(0, 300));
  });

  await anonymous.dispose();
};
