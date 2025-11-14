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

/**
 * Sets up paste handling in the main compose textarea to allow pasting quotes and media attachments.
 * Is called by Blazor when the compose dialog is opened.
 * @param dotnet
 * @param {HTMLTextAreaElement} textarea
 * @param {HTMLInputElement} fileInput
 */
export function setupPaste(dotnet, textarea, fileInput) {
    // Exit early if the textarea is somehow null to prevent a crash
    if (!textarea) return;
    // Exit early if the paste handling event listener is already set up
    if (textarea.hasAttribute("data-paste")) return;

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

    // Add attribute indicating paste handling is setup
    textarea.setAttribute("data-paste", true);
}

/**
 * @param {HTMLTextAreaElement} textarea
 * @param {string} text
 * @returns {string} The updated textarea contents
 */
export function insertText(textarea, text) {
    // Get the cursor position before inserting
    const pos = textarea.selectionStart;

    // Insert text
    textarea.setRangeText(text);

    // Move cursor to correct position
    textarea.setSelectionRange(pos + text.length, pos + text.length, "none");

    return textarea.value;
}