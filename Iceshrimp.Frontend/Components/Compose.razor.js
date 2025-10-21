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
        if (e.clipboardData.files.length === 0) {
            if (e.target.getAttribute("data-quote") === "True") return;

            const text = e.clipboardData.getData("text");

            try {
                const url = new URL(text);
                url.pathname = url.pathname.toLowerCase();

                // Check common note paths for Iceshrimp, Mastodon, Akkoma, Wafrn
                if (url.protocol.toLowerCase() === "https:" && (url.pathname.startsWith("/notes/") || url.pathname.startsWith("/@") || url.pathname.startsWith("/notice/") || url.pathname.startsWith("/objects/") || url.pathname.startsWith("/fediverse/post/"))) {
                    e.preventDefault();
                    await dotnet.invokeMethodAsync("PasteQuote", text);
                }
            } catch {
            }
        } else {
            e.preventDefault();
            fileInput.files = e.clipboardData.files;
            const changeEvent = new Event('change', {bubbles: true});
            fileInput.dispatchEvent(changeEvent);
        }
    });
}

/**
 * @param {HTMLTextAreaElement} textarea
 * @param {string | null} text
 */
export function insertText(textarea, text) {
    if (text === null) return;

    // Get the cursor position before inserting
    const pos = textarea.selectionStart;

    // Insert text
    textarea.setRangeText(text);

    // Move cursor to correct position
    textarea.setSelectionRange(pos + text.length, pos + text.length, "none");
}