// Loads list options from an IListOptionApiSource path in the browser, with the user's cookies, as the
// AngularJS optionSourceService did. Returns the response text for .NET to parse.
export async function getJson(path) {
    const response = await fetch(path, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
    if (!response.ok) {
        throw new Error(`${path} returned ${response.status}`);
    }
    return await response.text();
}
