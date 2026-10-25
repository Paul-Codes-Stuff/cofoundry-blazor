// TinyMCE for FormFieldHtml, the equivalent of the AngularJS ui-tinymce directive. TinyMCE 4.4.3 is the
// build the AngularJS admin bundles; its skin is already in shared.css, so skin loading is turned off.
// Changes are sent to .NET (debounced while typing, at once on change or blur, so a save straight after
// typing gets the latest html).
let loading;

// Editors being removed: their blur and change events must not call back into .NET.
const closing = new Set();

function loadTinyMce(scriptUrl) {
    if (window.tinymce) {
        return Promise.resolve(window.tinymce);
    }

    loading ??= new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = scriptUrl;
        script.onload = () => {
            window.tinymce.baseURL = scriptUrl.substring(0, scriptUrl.lastIndexOf('/'));
            resolve(window.tinymce);
        };
        script.onerror = () => reject(new Error(`${scriptUrl} could not be loaded`));
        document.head.appendChild(script);
    });

    return loading;
}

async function loadConfigFile(path) {
    if (!path) {
        return {};
    }

    const response = await fetch(path, { credentials: 'same-origin', headers: { 'Accept': 'application/json' } });
    return response.ok ? await response.json() : {};
}

// Reads what the insert image dialog needs from the image selected in the editor, as the AngularJS
// ImageAssetEditorDialog did.
function readImage(editor) {
    const node = editor.selection.getNode();
    if (!node || node.nodeName !== 'IMG') {
        return null;
    }

    let id = node.getAttribute('data-image-asset-id');
    if (!id) {
        // Fall back to the id at the start of the file name in the image's url.
        const src = node.getAttribute('src') || '';
        const fileName = src.substring(src.lastIndexOf('/') + 1);
        id = fileName.substring(0, fileName.indexOf('_'));
    }

    return {
        imageAssetId: parseInt(id, 10) || null,
        alt: node.getAttribute('alt'),
        width: node.style.width || node.getAttribute('width'),
        height: node.style.height || node.getAttribute('height')
    };
}

export async function init(element, scriptUrl, options, configPath, dotNetRef) {
    const tinymce = await loadTinyMce(scriptUrl);
    closing.delete(element.id);
    const fileOptions = await loadConfigFile(configPath);
    let timer;

    const send = () => {
        clearTimeout(timer);
        const editor = tinymce.get(element.id);
        if (editor && !closing.has(element.id)) {
            // The component may have been disposed since; nothing is lost if this fails.
            dotNetRef.invokeMethodAsync('OnEditorChanged', editor.getContent()).catch(() => { });
        }
    };
    const sendSoon = () => {
        clearTimeout(timer);
        timer = setTimeout(send, 300);
    };

    const settings = Object.assign({}, options.defaults, fileOptions, options.custom || {}, {
        target: element,
        skin: false,
        setup: editor => {
            editor.addButton('cfimage', {
                icon: 'image',
                tooltip: 'Insert image',
                onclick: () => dotNetRef.invokeMethodAsync('OnImageButton', readImage(editor)).catch(() => { })
            });
            editor.on('input keyup undo redo ExecCommand', sendSoon);
            editor.on('change blur', send);
        }
    });

    await tinymce.init(settings);
}

export function insertContent(id, html) {
    const editor = window.tinymce && window.tinymce.get(id);
    if (editor) {
        editor.insertContent(html);
        editor.fire('change');
    }
}

export function setContent(id, html) {
    const editor = window.tinymce && window.tinymce.get(id);
    if (editor && editor.getContent() !== html) {
        editor.setContent(html || '');
    }
}

export function destroy(id) {
    const editor = window.tinymce && window.tinymce.get(id);
    if (editor) {
        closing.add(id);
        try {
            editor.remove();
        } finally {
            closing.delete(id);
        }
    }
}
