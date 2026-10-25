// Port of shared.imageFileUtilities from the AngularJS admin. Reads the image chosen in a file
// input and, if it is larger than the maximum upload size, resizes it in the browser so the
// server never receives an image it would reject. Used by FormFieldImageUpload.razor.

const resizedImages = new WeakMap();
const previewUrls = new WeakMap();

/**
 * Reads the first file of the input. Returns null if no file is selected, otherwise its size, a
 * preview object url (null if the browser cannot display the format, e.g. TIFF) and whether it
 * was resized. A resized image is collected with getResizedImage().
 */
export async function readImage(input, maxWidth, maxHeight) {
    const file = input.files && input.files[0];
    release(input);

    if (!file) return null;

    const img = await loadImage(file);
    if (!img) {
        return { width: 0, height: 0, isResized: false, previewUrl: null };
    }

    const result = { width: img.naturalWidth, height: img.naturalHeight, isResized: false, previewUrl: img.src };

    if (file.type === 'image/svg+xml'
        || !((maxWidth && result.width > maxWidth) || (maxHeight && result.height > maxHeight))) {
        previewUrls.set(input, img.src);
        return result;
    }

    // Jpeg compresses photos far better than png, which is the only type canvas is required to support.
    const ratio = Math.min(getResizeRatio(result.width, maxWidth), getResizeRatio(result.height, maxHeight));
    const canvas = document.createElement('canvas');
    canvas.width = Math.round(ratio * result.width);
    canvas.height = Math.round(ratio * result.height);
    canvas.getContext('2d').drawImage(img, 0, 0, canvas.width, canvas.height);
    URL.revokeObjectURL(img.src);

    const blob = await new Promise(resolve => canvas.toBlob(resolve, 'image/jpeg'));
    const previewUrl = URL.createObjectURL(blob);
    resizedImages.set(input, blob);
    previewUrls.set(input, previewUrl);

    return { width: canvas.width, height: canvas.height, isResized: true, previewUrl };
}

/**
 * The bytes of the image resized by the last readImage() call, for .NET to read as a stream.
 */
export async function getResizedImage(input) {
    const blob = resizedImages.get(input);
    return blob ? new Uint8Array(await blob.arrayBuffer()) : new Uint8Array(0);
}

/**
 * Frees the preview url and resized image held for the input.
 */
export function release(input) {
    const url = previewUrls.get(input);
    if (url) URL.revokeObjectURL(url);
    previewUrls.delete(input);
    resizedImages.delete(input);
}

function loadImage(file) {
    return new Promise(resolve => {
        const img = document.createElement('img');
        img.onload = () => resolve(img);
        img.onerror = () => {
            URL.revokeObjectURL(img.src);
            resolve(null);
        };
        img.src = URL.createObjectURL(file);
    });
}

function getResizeRatio(size, max) {
    return max && size > max ? max / size : 1;
}
