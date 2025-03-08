export function openDialog(element) {
    element.show()
}

export function closeDialog(element) {
    element.close()
}

export function getSelectionStart(element) {
    return element.selectionStart;
}

export function openUpload(element) {
    element.click();
}

export function setupPaste(dotnet, textarea, fileInput) {
    textarea.addEventListener('paste', async (e) => {
        e.preventDefault();

        if (e.clipboardData.files.length === 0) {
            const text = e.clipboardData.getData("text");
            await dotnet.invokeMethodAsync("PasteQuote", text);
        } else {
            fileInput.files = e.clipboardData.files;
            const changeEvent = new Event('change', {bubbles: true});
            fileInput.dispatchEvent(changeEvent);
        }
    });
}