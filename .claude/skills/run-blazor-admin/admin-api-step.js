// Cofoundry.Web.Admin.Api, the copy of the admin API, served by the sandbox at /admin-next/api
// (Cofoundry:AdminApi:BaseUrl) next to Cofoundry.Web.Admin's original at /admin/api.
//   node .claude/skills/run-blazor-admin/driver.js run .claude/skills/run-blazor-admin/admin-api-step.js
// It compares GET responses from both copies, then adds, edits and deletes a role through the copy. It
// leaves nothing behind unless a step fails between the add and the delete (role "Admin API test <stamp>").
const NEW = '/admin-next/api/';
const OLD = '/admin/api/';

module.exports = async (page, c) => {
  const api = page.request;
  const json = async res => { try { return await res.json(); } catch { return null; } };
  let csrf;
  const write = (method, url, data) => api.fetch(url, { method, data, headers: { [csrf.header]: csrf.token } });

  await c.step('signed out: 401, not the admin or a page', async () => {
    const anon = await page.context().browser().newContext({ baseURL: new URL(page.url()).origin });
    const res = await anon.request.get(NEW + 'dashboard', { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
    await anon.close();
    if (res.status() !== 401) throw new Error('status ' + res.status());
  });

  await c.step('GETs match the original api', async () => {
    const firstId = async (path, key) => {
      const body = await json(await api.get(OLD + path));
      const items = body?.data?.items ?? body?.data;
      return items?.[0]?.[key];
    };
    const pageId = await firstId('pages', 'pageId');
    const directoryId = await firstId('page-directories', 'pageDirectoryId');
    const roleId = await firstId('roles', 'roleId');
    const imageId = await firstId('images', 'imageAssetId');
    const documentId = await firstId('documents', 'documentAssetId');
    const templateId = await firstId('page-templates', 'pageTemplateId');
    const customEntity = await firstId('custom-entity-definitions', 'customEntityDefinitionCode');
    const paths = [
      'account', 'dashboard', 'locales', 'permissions', 'user-areas', 'user-areas/COF/password-policy',
      'users?userAreaCode=COF', 'roles', `roles/${roleId}`, 'pages', `pages/${pageId}`, `pages/${pageId}/versions`,
      `pages/${pageId}/access-rules`, 'page-directories', 'page-directories/tree', `page-directories/${directoryId}`,
      `page-directories/${directoryId}/access-rules`, 'page-templates', `page-templates/${templateId}`, 'page-block-types',
      'images', `images/${imageId}`, 'images/settings', 'documents', `documents/${documentId}`, 'document-file-types',
      'settings/GeneralSite', 'settings/Seo', 'custom-entity-definitions', `custom-entity-definitions/${customEntity}`,
      `custom-entity-definitions/${customEntity}/routes`, `custom-entity-definitions/${customEntity}/data-model-schema`,
      `custom-entities?customEntityDefinitionCode=${customEntity}`, 'custom-entity-data-model-schemas',
      'custom-entity-routing-rules', 'nested-data-model-schemas',
    ];
    // Lists can be empty (e.g. no pages yet): skip their per-item paths.
    const skipped = paths.filter(p => p.includes('undefined'));
    if (skipped.length) console.log('       skipped, nothing to fetch: ' + skipped.join(', '));
    const mismatches = [];
    for (const path of paths.filter(p => !skipped.includes(p))) {
      const [a, b] = await Promise.all([api.get(OLD + path), api.get(NEW + path)]);
      const [ta, tb] = [await a.text(), await b.text()];
      if (a.status() !== 200 || b.status() !== 200 || ta !== tb) mismatches.push(`${path} (${a.status()}/${b.status()})`);
    }
    console.log(`       ${paths.length - skipped.length} endpoints compared`);
    if (mismatches.length) throw new Error('differ: ' + mismatches.join(', '));
  });

  await c.step('antiforgery: a write without the token is refused', async () => {
    const res = await api.post(NEW + 'roles', { data: { title: 'x', userAreaCode: 'COF' } });
    if (res.status() !== 400) throw new Error('status ' + res.status());
    // The AngularJS admin renders the request token into its pages; any page of the site's will do.
    await page.goto('/admin/settings');
    const html = await page.content();
    const token = /constant\('csrfToken', '([^']+)'\)/.exec(html)?.[1];
    const header = /constant\('csrfHeaderName', '([^']+)'\)/.exec(html)?.[1];
    if (!token || !header) throw new Error('no token in the AngularJS page');
    csrf = { token, header };
  });

  await c.step('validation errors come back as 400 with the errors', async () => {
    const res = await write('POST', NEW + 'roles', { title: '', userAreaCode: 'COF' });
    const body = await json(res);
    if (res.status() !== 400 || body?.isValid !== false || !body.errors?.some(e => /Title/i.test(e.properties?.join() + e.message))) {
      throw new Error(res.status() + ' ' + JSON.stringify(body).slice(0, 300));
    }
  });

  await c.step('add, edit and delete a role through the copy', async () => {
    const title = 'Admin API test ' + Date.now().toString(36);
    const added = await json(await write('POST', NEW + 'roles', { title, userAreaCode: 'COF', permissions: [] }));
    if (!added?.isValid || !added.data) throw new Error('add: ' + JSON.stringify(added).slice(0, 300));
    const roleId = added.data;
    try {
      const fromOld = await json(await api.get(`${OLD}roles/${roleId}`));
      if (fromOld?.data?.title !== title) throw new Error('the original api does not see the new role');
      const patched = await json(await write('PATCH', `${NEW}roles/${roleId}`, { title: title + ' (edited)' }));
      if (!patched?.isValid) throw new Error('patch: ' + JSON.stringify(patched).slice(0, 300));
      const edited = await json(await api.get(`${NEW}roles/${roleId}`));
      if (edited?.data?.title !== title + ' (edited)') throw new Error('title after patch: ' + edited?.data?.title);
    } finally {
      const deleted = await json(await write('DELETE', `${NEW}roles/${roleId}`));
      if (!deleted?.isValid) throw new Error('delete: ' + JSON.stringify(deleted).slice(0, 300));
    }
    const gone = await api.get(`${NEW}roles/${roleId}`);
    if (gone.status() !== 404) throw new Error('role still there: ' + gone.status());
  });

  await c.step('the Blazor admin still serves its pages', async () => {
    await page.goto('/admin-next/settings'); await c.settle();
    if (!/Application Name/i.test(await page.locator('body').innerText())) throw new Error('settings page did not load');
  });
};
