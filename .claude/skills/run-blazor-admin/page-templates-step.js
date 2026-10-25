// Page Templates (read-only module): list, filter by name, details with regions, and the page count link
// to the pages list filtered by template. Uses the sandbox's "General" template and creates nothing.
module.exports = async (page, c) => {
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  let detailsUrl;

  await c.step('page templates list renders', async () => {
    await page.goto('/admin-next/page-templates'); await c.settle();
    const rows = await c.rows();
    if (!rows.some(r => /^General\b/.test(r))) throw new Error(rows.slice(0, 5).join(' | '));
    await c.shot('templates-1-list');
  });

  await c.step('filter by name', async () => {
    await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await c.field('.search-filter', 'Name:').fill('General'); await c.field('.search-filter', 'Name:').blur();
    await c.btn('.search-filter', 'Apply').click(); await c.settle(1000);
    const rows = await c.rows();
    if (!rows.length || !rows.every(r => /General/i.test(r))) throw new Error(rows.join(' | '));
    await page.click('.page-header-buttons a.btn-icon:has-text("Filter")'); await page.waitForTimeout(600);
    await c.btn('.search-filter', 'Clear').click(); await c.settle(1000);
    if ((await c.rows()).length <= rows.length) throw new Error('clear did not restore the full list');
  });

  await c.step('details shows regions', async () => {
    await page.click('table.table a:text-is("General")'); await c.settle();
    detailsUrl = page.url();
    const text = await body();
    if (!/FILE PATH:/i.test(text) || !/PAGE TYPE: Generic/i.test(text) || !/Regions/.test(text)) throw new Error(text.slice(0, 400));
    if (await page.locator('.page-actions button, .page-actions input[type=submit]').count()) throw new Error('read-only module shows actions');
    await c.shot('templates-2-details');
  });

  await c.step('num pages links to the filtered pages list', async () => {
    const count = Number(await page.locator('.page-body label:has-text("Num Pages") + .controls a').innerText());
    await page.click('.page-body label:has-text("Num Pages") + .controls a'); await c.settle();
    if (!/\/admin-next\/pages\?pageTemplateId=\d+$/.test(page.url())) throw new Error(page.url());
    const rows = (await c.rows()).filter(r => !/no pages could be found/i.test(r));
    // The pages list shows 40 a page, so only check the count when it fits on one page.
    if (count <= 40 && rows.length !== count) throw new Error(`template has ${count} pages, list shows ${rows.length}`);
  });
};
