export function openDialog(element) {
    element.show()
}

export function closeDialog(element) {
    element.close()
}

export function openUpload(element) {
    element.click();
}

export function getSelectionStart(element) {
    return element.selectionStart;
}