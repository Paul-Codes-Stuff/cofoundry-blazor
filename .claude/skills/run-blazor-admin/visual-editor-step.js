// The Blazor visual editor on the site's pages (Cofoundry:AdminBlazor:UseVisualEditor, on in Dev.Sandbox).
// Creates a General page, then in edit mode: toolbar, add/edit/move/delete blocks through the iframe's dialogs,
// publish/unpublish and copy a version to draft. Then a blog post (a custom entity region): add, edit and delete
// a block and publish the post, and the same in its "Edit template" mode. Deletes what it creates at the end.
module.exports = async (page, c) => {
  const stamp = Date.now().toString(36);
  const title = `VisEd ${stamp}`;
  const slug = `vised-${stamp}`;
  const siteUrl = `/${slug}`;
  const frame = () => page.frameLocator('#cofoundry-src__iFrame');
  const modal = () => frame().locator('.modal').last();
  const frameShown = async () => page.locator('#cofoundry-src__iFrame').evaluate(el => getComputedStyle(el).display !== 'none');
  let blockSelector = '[data-cms-page-region-block]';
  const blocks = () => page.locator(blockSelector);
  let adminUrl;

  const openSite = async (query = '?mode=edit', url = siteUrl) => {
    const response = await page.goto(url + query);
    await page.waitForLoadState('load'); await page.waitForTimeout(800);
    return response;
  };

  // Waits for the frame's circuit, then its dialog.
  const waitForDialog = async text => {
    await modal().waitFor({ timeout: 15000 });
    if (text) await frame().locator(`.modal:has-text("${text}")`).first().waitFor({ timeout: 15000 });
    await page.waitForTimeout(500);
  };

  const waitForReload = async action => {
    await Promise.all([page.waitForNavigation({ timeout: 15000 }), action()]);
    await page.waitForLoadState('load'); await page.waitForTimeout(800);
  };

  // From outside: after a reload the mouse may already be over the block, which fires no mouseenter. And
  // c.shot takes full page screenshots, which resize the window: the editor redraws its overlays 500ms later.
  const hoverBlock = async index => {
    await page.mouse.move(0, 0); await page.waitForTimeout(700);
    // Clear of the site's fixed navbar, where Playwright's own scrolling can leave it, and at the bottom left:
    // the block's name label and the popover cover its top left, and its "+" (add below) the middle of a short
    // block. A plain mouse move, since a hover() keeps retrying while anything covers its point.
    const block = blocks().nth(index);
    await block.evaluate(el => window.scrollTo(0, Math.max(0, el.getBoundingClientRect().top + window.scrollY - 200)));
    const box = await block.boundingBox();
    await page.mouse.move(box.x + 3, box.y + Math.max(1, box.height - 3), { steps: 5 }); await page.waitForTimeout(300);
  };

  // Where the popover sits against the hovered block: over its top left corner, or above it (short blocks).
  const popoverPlacement = async index => {
    const block = await blocks().nth(index).boundingBox();
    const popover = await page.locator('#cofoundry-sv__block-popover-container').boundingBox();
    const near = (a, b) => Math.abs(a - b) <= 2;
    if (near(popover.y, block.y) && near(popover.x, block.x)) return 'over';
    if (near(popover.y + popover.height, block.y) && popover.x > block.x) return 'above';
    return `elsewhere: popover ${JSON.stringify(popover)}, block ${JSON.stringify(block)}`;
  };

  // Where a block's "+" (add below) sits: centred on its bottom edge, or past the bottom right corner of its
  // outline (short blocks, whose outline can be taller than the block: it has a min-height).
  const addButtonPlacement = async (index, outlineBottom) => {
    const block = await blocks().nth(index).boundingBox();
    const bottom = outlineBottom ? Math.max(block.y + block.height, block.y + 30) : block.y + block.height;
    const buttons = await page.locator('.cofoundry-sv__btn-add-block').evaluateAll(els => els
      .filter(el => el.offsetWidth && !el.classList.contains('cofoundry-sv__btn-add-block--empty'))
      .map(el => { const r = el.getBoundingClientRect(); return { x: r.x + window.scrollX, y: r.y + window.scrollY, width: r.width, height: r.height }; }));
    const scroll = await page.evaluate(() => ({ x: window.scrollX, y: window.scrollY }));
    const top = { x: block.x + scroll.x, y: bottom + scroll.y };
    const button = buttons.find(b => Math.abs(b.y + b.height / 2 - top.y) <= 2);
    if (!button) return 'no button on the bottom edge';
    const centre = button.x + button.width / 2;
    if (Math.abs(centre - (top.x + block.width / 2)) <= 2) return 'centred';
    if (button.x > top.x + block.width) return 'past the end';
    return `elsewhere: button ${JSON.stringify(button)}, block ${JSON.stringify(block)}`;
  };

  // Move, add above/below and delete are in the "..." menu, which opens on hover.
  const advanced = async id => {
    await page.locator('#cofoundry-sv__block-popover-container .advanced > button').hover(); await page.waitForTimeout(300);
    return page.locator('#' + id);
  };
  // In live mode the blocks have no data-cms-* attributes.
  const bodyText = () => page.locator('body').innerText();

  // TinyMCE's iframe inside the visual editor's iframe.
  const typeHtml = async text => {
    await modal().locator('.mce-tinymce').waitFor({ timeout: 15000 });
    const body = frame().frameLocator('.mce-tinymce iframe').locator('body');
    await body.click(); await body.press('Control+a'); await body.type(text);
  };

  await c.step('create a page', async () => {
    await page.goto('/admin-next/pages'); await c.settle();
    await page.click('.page-actions a:has-text("Create")'); await c.settle();
    await c.field('.page-body', 'Title:').fill(title);
    await c.field('.page-body', 'Title:').blur();
    await c.field('.page-body', 'Page Template:').selectOption({ label: 'General' });
    await c.field('.page-body', 'Directory:').selectOption({ label: '/' });
    await page.waitForTimeout(400);
    if ((await c.field('.page-body', 'Page Path:').inputValue()) !== slug) throw new Error('unexpected slug');
    await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
    if (!/\/admin-next\/pages\/\d+$/.test(page.url())) throw new Error('did not land on details: ' + page.url());
    adminUrl = new URL(page.url()).pathname;
  });

  try {
    await c.step('edit mode: toolbar, data, styles, no BOM', async () => {
      const response = await openSite();
      const body = await response.body();
      if (body[0] === 0xEF && body[1] === 0xBB && body[2] === 0xBF) throw new Error('response starts with a BOM');
      const html = body.toString('utf8');
      if (/Cofoundry\.PageResponseData/.test(html)) throw new Error('the AngularJS editor was injected too');
      if (!/<link[^>]+visual-editor\.css/.test(html)) throw new Error('no stylesheet link');
      if (!(await page.locator('#cofoundry-sv').count())) throw new Error('no toolbar');
      const data = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (data.visualEditorMode !== 'Edit' || data.isCustomEntityRoute || !data.pageId || !data.versionId || !data.canUpdate) throw new Error(JSON.stringify(data));
      // The toolbar is styled (fixed to the bottom) by visual-editor.css.
      const position = await page.locator('#cofoundry-sv').evaluate(el => getComputedStyle(el).position);
      if (position !== 'fixed') throw new Error('toolbar not styled: ' + position);
      // The sandbox sets Cofoundry:AdminBlazor:VisualEditorFontAwesome, and its site has no Font Awesome of its own:
      // the admin's copy is linked first and the icons get its font, which loads.
      if (!/<link[^>]+font-awesome\.css[^>]*>\s*<link[^>]+visual-editor\.css/.test(html)) throw new Error('no Font Awesome link before the editor stylesheet');
      const iconFont = await page.locator('#cofoundry-sv .fa').first().evaluate(el => getComputedStyle(el).fontFamily);
      if (!/FontAwesome/.test(iconFont)) throw new Error('icon font: ' + iconFont);
      const loaded = await page.evaluate(() => document.fonts.load('14px FontAwesome', '\uf040').then(fonts => fonts.length));
      if (!loaded) throw new Error('the Font Awesome font did not load');
      if (await frameShown()) throw new Error('frame shown before any action');
      if (!(await page.locator('.cofoundry-sv__ui-wrap--region').count())) throw new Error('no region overlay');
      if (!(await page.locator('.cofoundry-sv__options__button:has-text("Publish")').count())) throw new Error('no Publish button');
      await c.shot('ve-01-edit-mode');
    });

    await c.step('add a block to the empty region', async () => {
      await page.locator('[data-cms-page-region]').first().hover();
      await page.locator('.cofoundry-sv__btn-add-block--empty').click();
      await waitForDialog('Add Content Block to Body');
      if (!(await frameShown())) throw new Error('frame not shown');
      await c.shot('ve-02-add-step1');
      await modal().locator('tr:has-text("Content Section")').click();
      await modal().locator('button:text-is("Next")').click();
      await modal().getByLabel('Title:', { exact: true }).waitFor({ timeout: 15000 });
      // Save without the required text first: the dialog stays open with a message.
      await modal().locator('input[type=submit][value="Save"]').click(); await page.waitForTimeout(800);
      if (!/The Text field is required/.test(await modal().innerText())) throw new Error('no required message');
      // Content Section's [CustomEntityMultiTypeCollection]: a picker per type in its add menu.
      const entities = modal().locator('.control-group:has(label:text-is("Entities:"))');
      const pickers = await entities.locator('.menu-options button').allTextContents();
      if (pickers.map(t => t.trim()).join('|') !== 'Add or remove Category|Add or remove Blog Post') throw new Error('entities pickers: ' + pickers.join('|'));
      await modal().getByLabel('Title:', { exact: true }).fill(`First ${stamp}`);
      await modal().getByLabel('Title:', { exact: true }).blur();
      await typeHtml(`First text ${stamp}`);
      await c.shot('ve-03-add-step2');
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      if ((await blocks().count()) !== 1) throw new Error('blocks: ' + await blocks().count());
      if (!(await blocks().first().innerText()).includes(`First text ${stamp}`)) throw new Error('block content missing');
      if (await frameShown()) throw new Error('frame still shown after reload');
      await c.shot('ve-04-added');
    });

    await c.step('add a second block below', async () => {
      await hoverBlock(0);
      // A Content Section is a few lines high: the popover sits over its top left corner, as it always did.
      const placement = await popoverPlacement(0);
      if (placement !== 'over') throw new Error('popover ' + placement);
      const button = await addButtonPlacement(0);
      if (button !== 'centred') throw new Error('add below button ' + button);
      const addBelow = await advanced('cofoundry-sv__btn-block-addbelow');
      // Not c.shot: a full page screenshot resizes the window, which hides the popover.
      await page.screenshot({ path: require('path').join(process.env.OUT_DIR || require('path').join(require('os').tmpdir(), 'blazor-admin-run'), 've-04-popover.png') });
      await addBelow.click();
      await waitForDialog('Add Content Block');
      await modal().locator('tr:has-text("Content Section")').dblclick();
      await modal().getByLabel('Title:', { exact: true }).waitFor({ timeout: 15000 });
      await modal().getByLabel('Title:', { exact: true }).fill(`Second ${stamp}`);
      await modal().getByLabel('Title:', { exact: true }).blur();
      await typeHtml(`Second text ${stamp}`);
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      const texts = await blocks().allInnerTexts();
      if (texts.length !== 2 || !texts[0].includes('First') || !texts[1].includes('Second')) throw new Error(JSON.stringify(texts));
    });

    await c.step('cancel closes the dialog and hides the frame', async () => {
      await hoverBlock(0);
      await (await advanced('cofoundry-sv__btn-block-addabove')).click();
      await waitForDialog('Add Content Block');
      await modal().locator('button:text-is("Cancel")').click(); await page.waitForTimeout(800);
      if (await frameShown()) throw new Error('frame still shown');
    });

    await c.step('edit a block', async () => {
      await hoverBlock(1);
      await page.locator('#cofoundry-sv__btn-block-edit').click();
      await waitForDialog('Edit Content Block');
      const titleBox = modal().getByLabel('Title:', { exact: true });
      await titleBox.waitFor({ timeout: 15000 });
      if ((await titleBox.inputValue()) !== `Second ${stamp}`) throw new Error('title not loaded: ' + await titleBox.inputValue());
      await titleBox.fill(`Edited ${stamp}`); await titleBox.blur();
      await c.shot('ve-05-edit');
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      if (!(await blocks().nth(1).innerText()).includes(`Edited ${stamp}`)) throw new Error('edit not shown');
    });

    await c.step('move a block up', async () => {
      await hoverBlock(1);
      await waitForReload(async () => (await advanced('cofoundry-sv__btn-block-moveup')).click());
      const texts = await blocks().allInnerTexts();
      if (!texts[0].includes('Edited') || !texts[1].includes('First')) throw new Error(JSON.stringify(texts));
    });

    await c.step('delete a block', async () => {
      await hoverBlock(0);
      await (await advanced('cofoundry-sv__btn-block-delete')).click();
      await waitForDialog('Delete Block');
      await waitForReload(() => modal().locator('.main-cta').click());
      const texts = await blocks().allInnerTexts();
      if (texts.length !== 1 || !texts[0].includes('First')) throw new Error(JSON.stringify(texts));
    });

    await c.step('publish from the toolbar', async () => {
      await page.locator('.cofoundry-sv__options__button:has-text("Publish")').click();
      await waitForDialog('Publish Page');
      await c.shot('ve-06-publish');
      await waitForReload(() => modal().locator('.main-cta').click());
      // Publishing resets the mode: the page shows live.
      const data = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (data.visualEditorMode !== 'Live' || data.publishStatus !== 'Published') throw new Error(JSON.stringify(data));
      if (!(await page.locator('.cofoundry-sv__options__button:has-text("Unpublish")').count())) throw new Error('no Unpublish button');
      if (await page.locator('.cofoundry-sv__ui-wrap--region').count()) throw new Error('overlays in live mode');
      await c.shot('ve-07-live');
    });

    await c.step('signed out: no editor', async () => {
      const context = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin });
      try {
        const anonymous = await context.newPage();
        const response = await anonymous.goto(siteUrl + '?mode=edit');
        const html = await response.text();
        if (response.status() !== 200 || !html.includes(`First ${stamp}`)) throw new Error('published page not shown: ' + response.status());
        if (/cofoundry-sv|visual-editor\.(css|js)/.test(html)) throw new Error('editor injected for a signed out visitor');
      } finally {
        await context.close();
      }
    });

    await c.step('non-html responses are left alone', async () => {
      const response = await page.request.get('/test/test');
      const body = await response.body();
      if (body[0] === 0xEF) throw new Error('BOM added');
      if (/cofoundry-sv/.test(body.toString('utf8'))) throw new Error('editor injected into json');
      JSON.parse(body.toString('utf8'));
    });

    // ?version= of the live version redirects to live mode, so copy from an older one: publish again first.
    let firstVersionId;
    await c.step('publish a second version', async () => {
      firstVersionId = JSON.parse(await page.locator('#cofoundry-sv__data').textContent()).versionId;
      await openSite();
      const draft = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (draft.visualEditorMode !== 'Edit' || draft.versionId === firstVersionId) throw new Error('no new draft: ' + JSON.stringify(draft));
      await hoverBlock(0);
      await page.locator('#cofoundry-sv__btn-block-edit').click();
      await waitForDialog('Edit Content Block');
      await modal().getByLabel('Title:', { exact: true }).waitFor({ timeout: 15000 });
      await modal().getByLabel('Title:', { exact: true }).fill(`Second version ${stamp}`);
      await modal().getByLabel('Title:', { exact: true }).blur();
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      await page.locator('.cofoundry-sv__options__button:has-text("Publish")').click();
      await waitForDialog('Publish Page');
      await waitForReload(() => modal().locator('.main-cta').click());
      if (!(await bodyText()).includes(`Second version ${stamp}`)) throw new Error('second version not live');
    });

    await c.step('copy an old version to draft', async () => {
      await openSite(`?version=${firstVersionId}`);
      const data = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (data.visualEditorMode !== 'SpecificVersion' || data.hasDraftVersion) throw new Error(JSON.stringify(data));
      if (!(await page.locator('.cofoundry-sv__options__button:has-text("Copy to")').count())) throw new Error('no Copy to draft button');
      await c.shot('ve-08-version');
      // No draft yet, so it copies without asking and shows the page's default view (the new draft).
      await waitForReload(() => page.locator('.cofoundry-sv__options__button:has-text("Copy to")').click());
      const copy = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (!copy.hasDraftVersion || !(await bodyText()).includes(`First ${stamp}`)) throw new Error('not copied: ' + JSON.stringify(copy));
    });

    await c.step('copy over an existing draft asks first', async () => {
      await openSite(`?version=${firstVersionId}`);
      await page.locator('.cofoundry-sv__options__button:has-text("Copy to")').click();
      await waitForDialog('Copy Page Version');
      await c.shot('ve-09-copy-confirm');
      await waitForReload(() => modal().locator('.main-cta').click());
      if (!JSON.parse(await page.locator('#cofoundry-sv__data').textContent()).hasDraftVersion) throw new Error('no draft');
    });

    await c.step('unpublish from the toolbar', async () => {
      await openSite('?mode=live');
      await page.locator('.cofoundry-sv__options__button:has-text("Unpublish")').click();
      await waitForDialog('Unpublish Page');
      await waitForReload(() => modal().locator('.main-cta').click());
      await page.goto(adminUrl); await c.settle();
      if (!/unpublished/i.test(await c.status())) throw new Error('status: ' + await c.status());
    });
  } finally {
    await c.step('delete the page', async () => {
      if (!adminUrl) return;
      await page.goto(adminUrl); await c.settle();
      await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
      await page.click('.modal .main-cta'); await c.settle(2000);
      if (!/\/admin-next\/pages$/.test(page.url())) throw new Error('not back on list: ' + page.url());
    });
  }

  // A blog post's own region, on its details page.
  const postTitle = `VisEd post ${stamp}`;
  const control = label => page.locator(`.page-body .control-group:has(label:text-matches("^${label}$", "i"))`);
  const deleteEntity = async url => {
    await page.goto(url); await c.settle();
    await c.btn('.page-actions', 'Delete').click(); await page.waitForTimeout(800);
    await page.click('.modal .main-cta'); await c.settle(2000);
  };
  let authorUrl, postUrl, detailsPageUrl, postSiteUrl;

  try {
    await c.step('custom entity: create an author, a blog post and its details page', async () => {
      await page.goto('/admin-next/custom-entities/SIMAUT/new'); await c.settle();
      await c.field('.page-body', 'Name:').fill(`VisEd author ${stamp}`); await c.field('.page-body', 'Name:').blur();
      await c.btn('.page-actions', 'Save').click(); await c.settle(2000);   // authors aren't versioned
      if (!/\/SIMAUT\/\d+$/.test(page.url())) throw new Error('author not saved');
      authorUrl = new URL(page.url()).pathname;

      await page.goto('/admin-next/custom-entities/SIMBLP/new'); await c.settle();
      await c.field('.page-body', 'Title:').fill(postTitle); await c.field('.page-body', 'Title:').blur();
      const desc = control('Short description:').locator('textarea');
      await desc.fill('Visual editor test.'); await desc.blur();
      // Required: any existing image (the sandbox has some from the assets steps).
      await control('Thumbnail Image:').locator('button[title="Select"]').first().click(); await page.waitForTimeout(1500);
      const images = page.locator('.modal:has(.modal-title:text-is("Find an image"))');
      await images.locator('tbody tr').first().click(); await page.waitForTimeout(400);
      await images.locator('.page-actions button:text-is("Select")').click(); await page.waitForTimeout(1000);
      const search = control('Author:').locator('input.ui-select-search');
      await search.click(); await search.type(`VisEd author ${stamp}`); await page.waitForTimeout(1200);
      await control('Author:').locator('.ui-select-choices-row').first().dispatchEvent('mousedown'); await page.waitForTimeout(600);
      await c.btn('.page-actions', 'Save Draft').click(); await c.settle(2000);
      if (!/\/SIMBLP\/\d+$/.test(page.url())) throw new Error('post not saved');
      postUrl = new URL(page.url()).pathname;
      const postId = postUrl.split('/').pop();

      await page.goto('/admin-next/pages'); await c.settle();
      await page.click('.page-actions a:has-text("Create")'); await c.settle();
      await c.field('.page-body', 'Page Type:').selectOption({ label: 'Custom Entity Details' }); await page.waitForTimeout(800);
      await c.field('.page-body', 'Title:').fill(`VisEd posts ${stamp}`); await c.field('.page-body', 'Title:').blur();
      const templates = await c.field('.page-body', 'Page Template:').locator('option').allInnerTexts();
      const template = templates.find(t => /blog\s*post\s*details/i.test(t));
      if (!template) throw new Error('templates: ' + templates.join(' | '));
      await c.field('.page-body', 'Page Template:').selectOption({ label: template }); await page.waitForTimeout(800);
      await c.field('.page-body', 'Directory:').selectOption({ label: '/' });
      const formats = await c.field('.page-body', 'Route Format:').locator('option').allInnerTexts();
      const format = formats.find(f => f.includes('{Id}/{UrlSlug}'));
      if (!format) throw new Error('route formats: ' + formats.join(' | '));
      await c.field('.page-body', 'Route Format:').selectOption({ label: format }); await page.waitForTimeout(600);
      await c.btn('.page-actions', 'Save Draft').click(); await c.settle(3000);
      if (!/\/admin-next\/pages\/\d+$/.test(page.url())) throw new Error('details page not saved: ' + (await page.locator('.page-body').innerText()).slice(0, 400));
      detailsPageUrl = new URL(page.url()).pathname;
      postSiteUrl = `/${postId}/vised-post-${stamp}`;
    });

    await c.step('custom entity: add a block to the post', async () => {
      if (!postSiteUrl) throw new Error('no blog post');
      blockSelector = '[data-cms-custom-entity-region-block]';
      await openSite('?mode=edit', postSiteUrl);
      const data = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (!data.isCustomEntityRoute || data.entityNameSingular !== 'Blog Post' || !data.customEntityId) throw new Error(JSON.stringify(data));
      await page.mouse.move(0, 0); await page.waitForTimeout(700);
      await page.locator('.cofoundry-sv__btn-add-block--empty').click();
      await waitForDialog('Add Content Block to Body');
      // The region permits two block types.
      if ((await modal().locator('tbody tr').count()) !== 2) throw new Error('block types: ' + await modal().locator('tbody tr').count());
      await modal().locator('tr:has-text("Content Section")').dblclick();
      await modal().getByLabel('Title:', { exact: true }).waitFor({ timeout: 15000 });
      await modal().getByLabel('Title:', { exact: true }).fill(`Post block ${stamp}`);
      await modal().getByLabel('Title:', { exact: true }).blur();
      await typeHtml(`Post text ${stamp}`);
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      if ((await blocks().count()) !== 1 || !(await blocks().first().innerText()).includes(`Post block ${stamp}`)) throw new Error('block not added');
      await c.shot('ve-10-post-block');
    });

    await c.step('custom entity: edit the block', async () => {
      await hoverBlock(0);
      await page.locator('#cofoundry-sv__btn-block-edit').click();
      await waitForDialog('Edit Content Block');
      const titleBox = modal().getByLabel('Title:', { exact: true });
      await titleBox.waitFor({ timeout: 15000 });
      await titleBox.fill(`Post edited ${stamp}`); await titleBox.blur();
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      if (!(await blocks().first().innerText()).includes(`Post edited ${stamp}`)) throw new Error('edit not shown');
    });

    await c.step('custom entity: publish the post from the toolbar', async () => {
      await page.locator('.cofoundry-sv__options__button:has-text("Publish")').click();
      await waitForDialog('Publish Blog Post');
      await waitForReload(() => modal().locator('.main-cta').click());
      const data = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (data.visualEditorMode !== 'Live' || data.publishStatus !== 'Published') throw new Error(JSON.stringify(data));
    });

    await c.step('custom entity: delete the block', async () => {
      await openSite('?mode=edit', postSiteUrl);
      await hoverBlock(0);
      await (await advanced('cofoundry-sv__btn-block-delete')).click();
      await waitForDialog('Delete Block');
      await waitForReload(() => modal().locator('.main-cta').click());
      if (await blocks().count()) throw new Error('block not deleted');
    });

    // "Edit template" on a custom entity page (edittype=page): edits the details page's own regions, here the
    // template's "Page Templates" sidebar region, with the page's commands rather than the post's.
    await c.step('edit template: switch from the post', async () => {
      if (!postSiteUrl) throw new Error('no blog post');
      await openSite('?mode=edit', postSiteUrl);
      const switches = page.locator('.cofoundry-sv__mode__button__group a');
      if ((await switches.count()) !== 2) throw new Error('no edit type switch');
      if (!/Edit\s*Blog Post/i.test(await switches.nth(0).innerText())) throw new Error('first switch: ' + await switches.nth(0).innerText());
      await Promise.all([page.waitForNavigation(), switches.nth(1).click()]);
      await page.waitForLoadState('load'); await page.waitForTimeout(800);
      if (!/edittype=page/i.test(page.url())) throw new Error('url: ' + page.url());
      const data = JSON.parse(await page.locator('#cofoundry-sv__data').textContent());
      if (data.isCustomEntityRoute || data.visualEditorMode !== 'Edit' || data.entityNameSingular !== 'Page') throw new Error(JSON.stringify(data));
      if (`/admin-next/pages/${data.pageId}` !== detailsPageUrl) throw new Error('not the details page: ' + data.pageId);
      if ((await page.evaluate(() => document.documentElement.className)) !== 'cofoundry-editmode__page') throw new Error('html class');
      // Only the template's region gets an overlay; the post's region is dimmed.
      const names = await page.locator('.cofoundry-sv__ui-wrap--region .cofoundry-sv__ui-wrap-name').allTextContents();
      if (names.join('|') !== 'Page Templates') throw new Error('region overlays: ' + names.join('|'));
      const opacity = await page.locator('[data-cms-custom-entity-region]').evaluate(el => getComputedStyle(el).opacity);
      if (opacity !== '0.5') throw new Error('post region not dimmed: ' + opacity);
      await c.shot('ve-11-edit-template');
    });

    await c.step('edit template: add, edit and delete a block', async () => {
      blockSelector = '[data-cms-page-region-block]';
      await page.mouse.move(0, 0); await page.waitForTimeout(700);
      await page.locator('.cofoundry-sv__btn-add-block--empty').click();
      await waitForDialog('Add Content Block to Page Templates');
      await modal().locator('tr:has-text("Plain text")').dblclick();
      const text = modal().locator('.control-group:has(label:text-is("Plain text:")) textarea');
      await text.waitFor({ timeout: 15000 });
      await text.fill(`Template text ${stamp}`); await text.blur();
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      if (!/edittype=page/i.test(page.url())) throw new Error('edit type lost on reload: ' + page.url());
      if ((await blocks().count()) !== 1) throw new Error('blocks: ' + await blocks().count());
      if (!(await page.locator('[data-cms-page-region] [data-cms-page-region-block]').innerText()).includes(`Template text ${stamp}`)) throw new Error('block not in the template region');

      await hoverBlock(0);
      // A one line block: the popover goes above it, beside its name label, so the text stays visible.
      const placement = await popoverPlacement(0);
      if (placement !== 'above') throw new Error('popover ' + placement);
      // Its "+" (add below) goes past the end of the line rather than over the middle of the text.
      const button = await addButtonPlacement(0, true);
      if (button !== 'past the end') throw new Error('add below button ' + button);
      // It still adds below this block (the overlays are added regions first, so a block's button comes last).
      await page.locator('.cofoundry-sv__btn-add-block').last().click();
      await waitForDialog('Add Content Block');
      await modal().locator('button:text-is("Cancel")').click(); await page.waitForTimeout(800);
      if (await frameShown()) throw new Error('frame still shown');
      await page.screenshot({ path: require('path').join(process.env.OUT_DIR || require('path').join(require('os').tmpdir(), 'blazor-admin-run'), 've-12-short-block-popover.png') });
      await page.locator('#cofoundry-sv__btn-block-edit').click();
      await waitForDialog('Edit Content Block');
      const edit = modal().locator('.control-group:has(label:text-is("Plain text:")) textarea');
      await edit.waitFor({ timeout: 15000 });
      if ((await edit.inputValue()) !== `Template text ${stamp}`) throw new Error('text not loaded: ' + await edit.inputValue());
      await edit.fill(`Template edited ${stamp}`); await edit.blur();
      await waitForReload(() => modal().locator('input[type=submit][value="Save"]').click());
      if (!(await blocks().first().innerText()).includes(`Template edited ${stamp}`)) throw new Error('edit not shown');

      await hoverBlock(0);
      await (await advanced('cofoundry-sv__btn-block-delete')).click();
      await waitForDialog('Delete Block');
      await waitForReload(() => modal().locator('.main-cta').click());
      if (await blocks().count()) throw new Error('block not deleted');
    });

    await c.step('edit template: publish publishes the page', async () => {
      await page.locator('.cofoundry-sv__options__button:has-text("Publish")').click();
      await waitForDialog('Publish Page');
      await waitForReload(() => modal().locator('.main-cta').click());
      await page.goto(detailsPageUrl); await c.settle();
      if (!/^published$/i.test(await c.status())) throw new Error('details page status: ' + await c.status());
    });
  } finally {
    await c.step('custom entity: delete the page, post and author', async () => {
      if (detailsPageUrl) await deleteEntity(detailsPageUrl);
      if (postUrl) await deleteEntity(postUrl);
      if (authorUrl) await deleteEntity(authorUrl);
    });
  }
};
