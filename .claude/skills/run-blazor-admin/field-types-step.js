// Data model field types (phase 4) on the sandbox's dev-only Field Tests entity (SIMFLD,
// Cofoundry/CustomEntities/FieldTests). Fills every field, saves, and checks the stored model through the
// admin api (Cofoundry.Web.Admin.Api's /admin-next/api/custom-entities/{id}), so time zone conversion is checked against
// what the domain stored, not just what the form shows. Run it with BROWSER_TZ set to a zone that isn't
// UTC (e.g. America/New_York) so the "Local" fields actually convert. The asset fields upload their test
// files through the pickers. Deletes what it creates.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const title = `Fields ${stamp}`;
  const list = '/admin-next/field-tests';
  const body = async () => (await page.locator('.page-body').innerText()).replace(/\s+/g, ' ').trim();
  const messages = async () => (await page.locator('.field-validation-error, .validation-message').allInnerTexts()).join(' / ');
  const control = label => page.locator(`.page-body .control-group:has(label:text-matches("^${label}$", "i"))`).first();
  const input = label => control(label).locator('input').first();
  const set = async (label, value) => { await input(label).fill(value); await input(label).blur(); await page.waitForTimeout(150); };
  // What the browser itself makes of a local date/time, as the AngularJS admin would have sent it.
  const browserUtc = local => page.evaluate(l => new Date(l).toISOString(), local);
  const same = (a, b) => a != null && Date.parse(a) === Date.parse(b);
  // Radios and checkboxes are picked by their label.
  const choose = async (label, option) => { await control(label).locator(`label:text-is("${option}")`).click(); await page.waitForTimeout(200); };
  const optionTexts = async label => (await control(label).locator('select option, li label').allInnerTexts()).map(t => t.trim());
  // Enums may be stored by name or number, depending on the serializer settings.
  const isEnum = (actual, name, number) => actual === name || actual === number;
  let id;
  const stored = async () => {
    const res = await page.request.get(`/admin-next/api/custom-entities/${id}`);
    if (!res.ok()) throw new Error(`api ${res.status()}`);
    const json = await res.json();
    return (json.data || json).latestVersion.model;
  };

  await c.step('dates, time and colour: validation', async () => {
    console.log('[browser time zone] ' + await page.evaluate(() => Intl.DateTimeFormat().resolvedOptions().timeZone));
    await page.goto(`${list}/new`); await c.settle();
    await c.field('.page-body', 'Title:').fill(title); await c.field('.page-body', 'Title:').blur();
    if ((await input('Date as string:').getAttribute('min')) !== '2020-01-01') throw new Error('min not passed to the input');
    await set('Date as string:', '2019-06-01');
    await c.btn('.page-actions', 'Save').click(); await page.waitForTimeout(1000);
    const msgs = await messages();
    if (!/The Date field is required/.test(msgs) || !/Date as string is not in a valid range/.test(msgs)
      || !/The Enum radio field is required/.test(msgs)) throw new Error('messages: ' + msgs);
  });

  await c.step('dates, time and colour: save', async () => {
    await set('Date:', '2024-05-06');
    await set('Date local:', '2024-07-01');
    await set('Date as string:', '2024-02-29');
    await set('Date and time:', '2024-03-04T05:06');
    await set('Date and time local:', '2024-07-01T12:00:30');
    await set('Plain date time:', '2024-08-09T10:11');
    await set('Time:', '09:30');
    await set('Time as string:', '13:14:15');
    await set('Color:', '#ff8800');
    if (!/#ff8800/i.test(await control('Color:').innerText())) throw new Error('colour swatch not updated');
    await choose('Enum radio:', 'Red');
    await c.shot('fields-1-dates');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!/\/field-tests\/\d+$/.test(page.url())) throw new Error('not saved: ' + await messages());
    id = page.url().match(/(\d+)$/)[1];
    const text = await body();
    // Dates use the server's culture ("06 May 2024" or "May 6, 2024"), so only check the parts.
    for (const expected of [/DATE: [^:]*?(0?6 May|May 6),? 2024/, /July.*2024 12:00:30/, /TIME: 09:30/, /13:14:15/, /#ff8800/]) {
      if (!expected.test(text)) throw new Error(`${expected} not shown: ` + text.slice(0, 600));
    }
    await c.shot('fields-2-dates-view');
  });

  await c.step('dates, time and colour: stored values', async () => {
    const m = await stored();
    const checks = [
      ['date', m.date, '2024-05-06T00:00:00Z'],
      ['dateLocal', m.dateLocal, await browserUtc('2024-07-01T00:00:00')],
      ['dateAsString', m.dateAsString, '2024-02-29T00:00:00Z'],
      ['dateAndTime', m.dateAndTime, '2024-03-04T05:06:00Z'],
      ['dateAndTimeLocal', m.dateAndTimeLocal, await browserUtc('2024-07-01T12:00:30')],
      ['plainDateTime', m.plainDateTime, '2024-08-09T10:11:00Z'],
    ];
    const wrong = checks.filter(([, actual, expected]) => !same(actual, expected)).map(([n, a, e]) => `${n}: ${a} (expected ${e})`);
    if (m.time !== '09:30:00') wrong.push(`time: ${m.time}`);
    if (m.timeAsString !== '13:14:15') wrong.push(`timeAsString: ${m.timeAsString}`);
    if (m.color !== '#ff8800') wrong.push(`color: ${m.color}`);
    if (wrong.length) throw new Error(wrong.join('; '));
  });

  await c.step('dates, time and colour: edit shows the entered values', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(800);
    const values = {
      'Date:': '2024-05-06', 'Date local:': '2024-07-01', 'Date and time local:': '2024-07-01T12:00:30',
      'Date and time:': '2024-03-04T05:06', 'Time:': '09:30', 'Time as string:': '13:14:15', 'Color:': '#ff8800',
    };
    const wrong = [];
    for (const [label, expected] of Object.entries(values)) {
      const actual = await input(label).inputValue();
      if (!actual.startsWith(expected)) wrong.push(`${label} ${actual} (expected ${expected})`);
    }
    if (wrong.length) throw new Error(wrong.join('; '));
    await set('Date local:', '');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if ((await stored()).dateLocal != null) throw new Error('clearing a date did not store null');
  });

  await c.step('option lists: options', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    const expected = {
      'Enum dropdown:': 'No colour|Red|Leafy green|Blue',
      'Number dropdown:': '|One|Two|Three',
      'Api dropdown:': '|Dog|Cat|Llama',
      'Enum radio:': 'Red|Leafy green|Blue',
      'Api radio:': 'No pet|Dog|Cat|Llama',
      'Enum checkboxes:': 'Red|Leafy green|Blue',
      'Api checkboxes:': 'Dog|Cat|Llama',
    };
    const wrong = [];
    for (const [label, options] of Object.entries(expected)) {
      const actual = (await optionTexts(label)).join('|');
      if (actual !== options) wrong.push(`${label} ${actual} (expected ${options})`);
    }
    if (wrong.length) throw new Error(wrong.join('; '));
  });

  await c.step('option lists: save', async () => {
    await control('Enum dropdown:').locator('select').selectOption({ label: 'Leafy green' });
    await control('Number dropdown:').locator('select').selectOption({ label: 'Two' });
    await control('Api dropdown:').locator('select').selectOption({ label: 'Llama' });
    await choose('Enum radio:', 'Blue');
    await choose('Api radio:', 'Cat');
    await choose('Enum checkboxes:', 'Red'); await choose('Enum checkboxes:', 'Blue');
    await choose('Api checkboxes:', 'Dog'); await choose('Api checkboxes:', 'Llama');
    await c.shot('fields-3-lists');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const text = await body();
    for (const expected of [/ENUM DROPDOWN: Leafy green/, /NUMBER DROPDOWN: Two/, /API DROPDOWN: Llama/, /ENUM RADIO: Blue/,
      /API RADIO: Cat/, /ENUM CHECKBOXES: Red Blue/, /API CHECKBOXES: Dog Llama/]) {
      if (!expected.test(text)) throw new Error(`${expected} not shown: ` + text.slice(0, 900));
    }
    const m = await stored();
    const wrong = [];
    if (!isEnum(m.enumDropdown, 'Green', 1)) wrong.push('enumDropdown ' + m.enumDropdown);
    if (m.numberDropdown !== 2) wrong.push('numberDropdown ' + m.numberDropdown);
    if (m.apiDropdown !== 3) wrong.push('apiDropdown ' + m.apiDropdown);
    if (!isEnum(m.enumRadio, 'Blue', 2)) wrong.push('enumRadio ' + m.enumRadio);
    if (m.apiRadio !== 2) wrong.push('apiRadio ' + m.apiRadio);
    if (JSON.stringify(m.enumCheckboxes) !== '["Red","Blue"]' && JSON.stringify(m.enumCheckboxes) !== '[0,2]') wrong.push('enumCheckboxes ' + JSON.stringify(m.enumCheckboxes));
    if (JSON.stringify(m.apiCheckboxes) !== '[1,3]') wrong.push('apiCheckboxes ' + JSON.stringify(m.apiCheckboxes));
    if (wrong.length) throw new Error(wrong.join('; '));
  });

  await c.step('option lists: clear', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    await control('Enum dropdown:').locator('select').selectOption({ label: 'No colour' });
    await choose('Api radio:', 'No pet');
    await choose('Enum checkboxes:', 'Red'); await choose('Enum checkboxes:', 'Blue');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    if (m.enumDropdown != null || m.apiRadio != null || m.enumCheckboxes.length) throw new Error(JSON.stringify(m));
    if (!/ENUM CHECKBOXES: No colours/.test(await body())) throw new Error('no value text not shown');
  });

  // Asset fields. Test files are generated in the browser, uploaded through the pickers' upload dialogs
  // and deleted at the end.
  const makePng = async (w, h, colour) => Buffer.from(await page.evaluate(([w, h, colour]) => {
    const cv = document.createElement('canvas'); cv.width = w; cv.height = h;
    const ctx = cv.getContext('2d'); ctx.fillStyle = colour; ctx.fillRect(0, 0, w, h);
    return cv.toDataURL('image/png').split(',')[1];
  }, [w, h, colour]), 'base64');
  const top = () => page.locator('.modal').last();
  // Stacked dialogs: the upload dialog's buttons, not the picker's underneath it.
  const topBtn = text => top().locator(`button:text-is("${text}"), input[type=submit][value="${text}"]`).first();
  const icon = (scope, title) => scope.locator(`.btn-icon[title="${title}"]`).first();
  const assetRows = async label => (await control(label).locator('tbody tr').allInnerTexts()).map(t => t.replace(/\s+/g, ' ').trim());
  const upload = async (file, title) => {
    await top().locator('.collection-actions a[title="Upload"]').click(); await page.waitForTimeout(800);
    await top().locator('input[type=file]').setInputFiles(file); await page.waitForTimeout(800);
    await top().getByLabel('Title:', { exact: true }).fill(title); await top().getByLabel('Title:', { exact: true }).blur();
    await topBtn('Upload').click(); await page.waitForTimeout(2000);
  };
  const assets = { img: [`Fld img a ${stamp}`, `Fld img b ${stamp}`, `Fld img min ${stamp}`], doc: [`Fld doc txt ${stamp}`, `Fld doc pdf ${stamp}`] };
  const pdf = Buffer.from('%PDF-1.4\n1 0 obj<</Type/Catalog/Pages 2 0 R>>endobj 2 0 obj<</Type/Pages/Kids[]/Count 0>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF\n');
  let saved;

  await c.step('asset fields: pickers apply the attribute filters', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    await icon(control('Min size image:'), 'Select').click(); await page.waitForTimeout(1200);
    if (!/Find an image/.test(await top().innerText())) throw new Error('image picker not open');
    await c.btn('.modal', 'Filter').click(); await page.waitForTimeout(500);
    const minWidth = top().getByLabel('Min Width:', { exact: true });
    if ((await minWidth.inputValue()) !== '50' || !(await minWidth.isDisabled())) throw new Error('min width not preset');
    if (await top().getByLabel('Width:', { exact: true }).count()) throw new Error('width shown alongside min width');
    await c.btn('.modal', 'Cancel').click(); await page.waitForTimeout(800);
    await icon(control('Pdf document:'), 'Select').click(); await page.waitForTimeout(1200);
    await c.btn('.modal', 'Filter').click(); await page.waitForTimeout(500);
    const ext = top().locator('select');
    if ((await ext.inputValue()) !== 'pdf' || !(await ext.isDisabled())) throw new Error('extension not preset: ' + await ext.inputValue());
    await c.btn('.modal', 'Cancel').click(); await page.waitForTimeout(800);
    if (await page.locator('.modal').count()) throw new Error('picker not closed');
  });

  await c.step('asset fields: upload and pick', async () => {
    // Image collection: two uploads, each added and the picker closed.
    for (const [i, title] of assets.img.slice(0, 2).entries()) {
      await control('Images:').locator('.collection-actions a[title="Add"]').click(); await page.waitForTimeout(1200);
      await upload({ name: `fld-${i}-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(80, 60, i ? '#36c' : '#c33') }, title);
    }
    let rows = await assetRows('Images:');
    if (rows.length !== 2 || !rows[0].includes(assets.img[0]) || !rows[1].includes(assets.img[1])) throw new Error(rows.join(' | '));
    // Single image with a size restriction: the upload dialog warns about it.
    await icon(control('Min size image:'), 'Select').click(); await page.waitForTimeout(1200);
    await top().locator('.collection-actions a[title="Upload"]').click(); await page.waitForTimeout(800);
    if (!/This image field has restrictions: min-size 50x50/.test(await top().innerText())) throw new Error('no restriction warning');
    await topBtn('Cancel').click(); await page.waitForTimeout(500);
    await upload({ name: `fld-min-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(100, 100, '#3a3') }, assets.img[2]);
    if (await page.locator('.modal').count()) throw new Error('picker still open after upload');
    if (!(await control('Min size image:').locator('img').count())) throw new Error('no preview image');
    // PDF document (file type warning), then the collection: upload a text file and pick the pdf too.
    await icon(control('Pdf document:'), 'Select').click(); await page.waitForTimeout(1200);
    await top().locator('.collection-actions a[title="Upload"]').click(); await page.waitForTimeout(800);
    if (!/has restrictions/.test(await top().innerText())) throw new Error('no file type warning');
    await topBtn('Cancel').click(); await page.waitForTimeout(500);
    await upload({ name: `fld-${stamp}.pdf`, mimeType: 'application/pdf', buffer: pdf }, assets.doc[1]);
    if (!/\.pdf/.test(await control('Pdf document:').innerText())) throw new Error('pdf not shown: ' + await control('Pdf document:').innerText());
    await control('Documents:').locator('.collection-actions a[title="Add"]').click(); await page.waitForTimeout(1200);
    await upload({ name: `fld-${stamp}.txt`, mimeType: 'text/plain', buffer: Buffer.from('field test\n') }, assets.doc[0]);
    await control('Documents:').locator('.collection-actions a[title="Add"]').click(); await page.waitForTimeout(1200);
    if ((await top().locator('tr.selected').count()) !== 1) throw new Error('current document not selected in the picker');
    await top().locator(`tr:has(td:text-is("${assets.doc[1]}"))`).click(); await page.waitForTimeout(300);
    await c.btn('.modal', 'Ok').click(); await page.waitForTimeout(1200);
    rows = await assetRows('Documents:');
    if (rows.length !== 2 || !/\.txt/.test(rows[0]) || !/\.pdf/.test(rows[1])) throw new Error(rows.join(' | '));
    await c.shot('fields-4-assets');
  });

  await c.step('asset fields: drag, save, stored values', async () => {
    const trs = control('Images:').locator('tbody tr');
    await trs.nth(1).dragTo(trs.nth(0)); await page.waitForTimeout(800);
    const rows = await assetRows('Images:');
    if (!rows[0].includes(assets.img[1])) throw new Error('not reordered: ' + rows.join(' | '));
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    saved = await stored();
    const wrong = [];
    if (!(saved.minSizeImage > 0)) wrong.push('minSizeImage ' + saved.minSizeImage);
    if (saved.image != null) wrong.push('image ' + saved.image);
    if (saved.images.length !== 2) wrong.push('images ' + JSON.stringify(saved.images));
    if (!(saved.pdfDocument > 0)) wrong.push('pdfDocument ' + saved.pdfDocument);
    if (saved.documents.length !== 2 || saved.documents[1] !== saved.pdfDocument) wrong.push('documents ' + JSON.stringify(saved.documents));
    if (wrong.length) throw new Error(wrong.join('; '));
    // Read-only view: the collections show their rows without actions.
    if (!(await assetRows('Images:'))[0].includes(assets.img[1]) || await control('Images:').locator('.btn-icon').count()) throw new Error('read-only view');
  });

  await c.step('asset fields: remove', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    await icon(control('Images:').locator('tbody tr').first(), 'Remove').click(); await page.waitForTimeout(500);
    await icon(control('Min size image:'), 'Remove').click(); await page.waitForTimeout(500);
    await icon(control('Pdf document:'), 'Remove').click(); await page.waitForTimeout(500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    if (m.minSizeImage != null || m.pdfDocument != null || JSON.stringify(m.images) !== JSON.stringify([saved.images[1]])) throw new Error(JSON.stringify(m));
    if (!/PDF DOCUMENT: None/i.test(await body())) throw new Error('removed document not shown as None');
  });

  // Page and directory fields use the sandbox's existing pages and directories (the smoke test creates
  // pages), so these steps don't create any.
  await c.step('page and directory fields: pick and save', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    const search = control('Page:').locator('input.ui-select-search');
    await search.click(); await page.waitForTimeout(1200);
    const choices = control('Page:').locator('.ui-select-choices-row');
    if ((await choices.count()) < 2) throw new Error('page selector needs at least two pages');
    const pageText = (await choices.first().innerText()).trim();
    await choices.first().dispatchEvent('mousedown'); await page.waitForTimeout(600);
    if ((await control('Page:').locator('.ui-select-match').innerText()).trim() !== pageText) throw new Error('page not selected');
    await control('Pages:').locator('.collection-actions a[title="Add"]').click(); await page.waitForTimeout(1200);
    if (!/Find Page/.test(await top().innerText())) throw new Error('page picker not open');
    await top().locator('tbody tr').nth(0).click(); await top().locator('tbody tr').nth(1).click(); await page.waitForTimeout(300);
    if ((await top().locator('tr.selected').count()) !== 2) throw new Error('two rows not selected');
    await topBtn('Ok').click(); await page.waitForTimeout(1200);
    if ((await assetRows('Pages:')).length !== 2) throw new Error('pages: ' + (await assetRows('Pages:')).join(' | '));
    if (await control('Pages:').locator('tbody tr[draggable=true]').count()) throw new Error('unordered collection is draggable');
    const directory = control('Directory:').locator('select');
    await directory.selectOption({ index: 1 }); await page.waitForTimeout(300);
    await c.shot('fields-5-pages');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    if (!(m.page > 0) || m.pages.length !== 2 || !(m.directory > 0)) throw new Error(JSON.stringify({ page: m.page, pages: m.pages, directory: m.directory }));
    if (!(await control('Page:').locator(`a:text-is("${pageText}")`).count())) throw new Error('read-only page link missing');
  });

  await c.step('page and directory fields: clear', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    await control('Page:').locator('button[title="Clear"]').click(); await page.waitForTimeout(300);
    await icon(control('Pages:').locator('tbody tr').first(), 'Remove').click(); await page.waitForTimeout(300);
    await control('Directory:').locator('select').selectOption({ label: 'None' }); await page.waitForTimeout(300);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    if (m.page != null || m.pages.length !== 1 || m.directory != null) throw new Error(JSON.stringify({ page: m.page, pages: m.pages, directory: m.directory }));
  });

  // [CustomEntityMultiTypeCollection] (Related: categories and authors, orderable). Creates two categories and
  // an author, deleted at the end.
  const related = { categories: [`Fld cat A ${stamp}`, `Fld cat B ${stamp}`], author: `Fld author ${stamp}`, urls: [] };
  const relatedRows = async () => (await control('Related:').locator('tbody tr').allInnerTexts()).map(t => t.replace(/\s+/g, ' ').trim());
  const pickRelated = async (type, names) => {
    await control('Related:').locator('.collection-actions .menu').hover(); await page.waitForTimeout(300);
    await control('Related:').locator(`.menu-options button:text-is("Add or remove ${type}")`).click(); await page.waitForTimeout(1500);
    if (!new RegExp(`Find ${type}`, 'i').test(await top().locator('.modal-title').innerText())) throw new Error(`no ${type} picker`);
    for (const name of names) {
      await top().locator(`tr:has(a:text-is("${name}"))`).click({ position: { x: 5, y: 5 } }); await page.waitForTimeout(300);
    }
    await topBtn('Ok').click(); await page.waitForTimeout(1500);
  };

  await c.step('multi-type collection: pick both types, save', async () => {
    for (const name of related.categories) {
      await page.goto('/admin-next/categories/new'); await c.settle();
      await c.field('.page-body', 'Title:').fill(name); await c.field('.page-body', 'Title:').blur();
      await control('Short Description:').locator('textarea').fill(`About ${name}`); await control('Short Description:').locator('textarea').blur();
      await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
      if (!/\/categories\/\d+$/.test(page.url())) throw new Error('category not saved');
      related.urls.push(new URL(page.url()).pathname);
    }
    await page.goto('/admin-next/authors/new'); await c.settle();
    await c.field('.page-body', 'Name:').fill(related.author); await c.field('.page-body', 'Name:').blur();
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    if (!/\/authors\/\d+$/.test(page.url())) throw new Error('author not saved');
    related.urls.push(new URL(page.url()).pathname);

    await page.goto(`${list}/${id}`); await c.settle();
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    const headers = (await control('Related:').locator('thead th').allInnerTexts()).map(t => t.trim().toLowerCase());
    // Categories have a preview image and description; neither type has a status (both auto-publish).
    if (headers.join('|') !== '|type|name|description|actions') throw new Error('headers: ' + headers.join('|'));
    await pickRelated('Category', related.categories);
    await pickRelated('Author', [related.author]);
    const rows = await relatedRows();
    if (rows.length !== 3 || !rows[0].startsWith(`Category ${related.categories[0]}`) || !rows[2].startsWith(`Author ${related.author}`)) throw new Error('rows: ' + rows.join(' | '));
    if (!rows[0].includes(`About ${related.categories[0]}`)) throw new Error('no description: ' + rows[0]);
    if ((await control('Related:').locator('tbody tr[draggable=true]').count()) !== 3) throw new Error('orderable rows not draggable');
    await c.shot('fields-5b-related');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    const codes = (m.related || []).map(r => r.customEntityDefinitionCode).join(',');
    if (codes !== 'SIMCAT,SIMCAT,SIMAUT' || m.related.some(r => !(r.customEntityId > 0))) throw new Error(JSON.stringify(m.related));
  });

  await c.step('multi-type collection: reorder, re-pick one type, remove', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    const rows = control('Related:').locator('tbody tr');
    await rows.nth(2).dragTo(rows.nth(0)); await page.waitForTimeout(800);
    let texts = await relatedRows();
    if (!texts[0].startsWith('Author')) throw new Error('not reordered: ' + texts.join(' | '));
    // Deselect category A in its picker: the author keeps its place and category B stays.
    await pickRelated('Category', [related.categories[0]]);
    texts = await relatedRows();
    if (texts.length !== 2 || !texts[0].startsWith('Author') || !texts[1].includes(related.categories[1])) throw new Error('after picker: ' + texts.join(' | '));
    await icon(control('Related:').locator('tbody tr').first(), 'Remove').click(); await page.waitForTimeout(500);
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    if (m.related.length !== 1 || m.related[0].customEntityDefinitionCode !== 'SIMCAT') throw new Error(JSON.stringify(m.related));
    // Read-only view: the title links to the entity, no actions column.
    if (!(await control('Related:').locator(`a:text-is("${related.categories[1]}")`).count())) throw new Error('read-only link missing');
    if (await control('Related:').locator('.collection-actions').count()) throw new Error('add menu in read-only view');
  });

  // The html editor (TinyMCE): type, insert an uploaded image with the image button, save.
  let htmlImageId;
  await c.step('html field: type, insert an image, save', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    const editor = control('Html:').locator('.mce-tinymce');
    await editor.waitFor({ timeout: 15000 });
    if (!(await control('Html:').locator('.mce-btn[aria-label="Insert image"]').count())) throw new Error('no image button');
    if (!(await control('Html:').locator('.mce-btn[aria-label="Source code"]').count())) throw new Error('no source button');
    const body = page.frameLocator('.page-body .control-group:has(label:text-matches("^Html:$", "i")) iframe').locator('body');
    await body.click(); await page.keyboard.type(`Hello html ${stamp}`); await page.keyboard.press('Enter');
    await control('Html:').locator('.mce-btn[aria-label="Insert image"] button').click(); await page.waitForTimeout(1200);
    if (!/Insert an image/.test(await top().innerText())) throw new Error('insert image dialog not open');
    await icon(top(), 'Select').click(); await page.waitForTimeout(1200);
    await upload({ name: `fld-html-${stamp}.png`, mimeType: 'image/png', buffer: await makePng(90, 90, '#93c') }, `Fld img html ${stamp}`);
    if ((await top().getByLabel('Alt tag:', { exact: true }).inputValue()) !== `Fld img html ${stamp}`) throw new Error('alt text not set from the image title');
    await top().getByLabel('Width:', { exact: true }).fill('60'); await top().getByLabel('Width:', { exact: true }).blur();
    await topBtn('Insert').click(); await page.waitForTimeout(1000);
    if (await page.locator('.modal').count()) throw new Error('insert dialog still open');
    await c.shot('fields-6-html');
    await c.btn('.page-actions', 'Save').click(); await c.settle(2000);
    const m = await stored();
    if (!m.html || !m.html.includes(`Hello html ${stamp}`) || !/<img [^>]*data-image-asset-id="\d+"/.test(m.html) || !/width: 60px/.test(m.html)) throw new Error('html: ' + m.html);
    htmlImageId = parseInt(m.html.match(/data-image-asset-id="(\d+)"/)[1], 10);
    if (!(await control('Html:').locator('.ta-text img').count())) throw new Error('read-only view does not render the html');
  });

  await c.step('html field: edit again keeps the content', async () => {
    await c.btn('.page-actions', 'Edit').click(); await page.waitForTimeout(1500);
    await control('Html:').locator('.mce-tinymce').waitFor({ timeout: 15000 });
    const body = page.frameLocator('.page-body .control-group:has(label:text-matches("^Html:$", "i")) iframe').locator('body');
    if (!(await body.innerText()).includes(`Hello html ${stamp}`)) throw new Error('editor lost its content');
    await c.btn('.page-actions', 'Cancel').click(); await page.waitForTimeout(1200);
  });

  await c.step('unsupported field type shows a placeholder', async () => {
    const text = await control('Unsupported:').innerText();
    if (!/This field type \(NotARealFieldType\) can't be edited in this admin yet/.test(text)) throw new Error(text);
  });

  await c.step('cleanup', async () => {
    await page.goto(list); await c.settle();
    if (!(await page.locator(`table.table a:text-is("${title}")`).count())) return;
    await page.click(`table.table a:text-is("${title}")`); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
    if (!page.url().endsWith(list)) throw new Error('delete failed: ' + (await body()).slice(0, 300));
  });

  await c.step('cleanup related entities', async () => {
    for (const url of related.urls) {
      await page.goto(url); await c.settle();
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(1500);
      if (/\/\d+$/.test(page.url())) throw new Error(`delete ${url} failed`);
    }
  });

  await c.step('cleanup assets', async () => {
    if (!saved) return;
    const targets = [...new Set([saved.minSizeImage, ...saved.images, htmlImageId].filter(i => i))].map(i => `images/${i}`)
      .concat([...new Set(saved.documents)].map(d => `documents/${d}`));
    for (const target of targets) {
      await page.goto(`/admin-next/${target}`); await c.settle();
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(1500);
      if (/\/\d+$/.test(page.url())) throw new Error(`delete ${target} failed`);
    }
  });
};
