// The visual editor frame's side of the messages with js/visual-editor.js on the site's page, used by
// Components/VisualEditor/VisualEditorFrame.razor. The frame is same-origin with the page that holds it.

let listener = null;

/**
 * Passes the page's actions to the frame component and tells the page the frame is ready for them.
 */
export function init(dotNetFrame) {
    document.body.classList.add('cofoundry-visual-editor-frame');

    listener = e => {
        if (e.origin !== window.location.origin || e.source !== window.parent || !e.data || !e.data.action) return;

        dotNetFrame.invokeMethodAsync('HandleMessage', e.data.action, JSON.stringify(e.data.args || {}), JSON.stringify(e.data.context || {}));
    };
    window.addEventListener('message', listener);

    notifyParent('FRAME_READY');
}

/**
 * Hides the frame again, once its dialog has closed.
 */
export function closeDialog() {
    notifyParent('MODAL_CLOSE');
}

/**
 * Reloads the page, after a change to its content. With resetMode, also leaves the version or mode being
 * viewed (as after publishing), which shows the page's default view.
 */
export function reloadParent(resetMode) {
    const location = window.parent.location;

    if (!resetMode) {
        location.reload();
        return;
    }

    const url = new URL(location.href);
    url.searchParams.delete('mode');
    url.searchParams.delete('version');
    location.href = url.toString();
}

export function dispose() {
    if (listener) window.removeEventListener('message', listener);
    listener = null;
}

function notifyParent(type) {
    if (window.parent !== window) {
        window.parent.postMessage({ type: type }, window.location.origin);
    }
}
