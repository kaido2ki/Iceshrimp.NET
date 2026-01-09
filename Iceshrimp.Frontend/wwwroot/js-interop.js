/**
 * Displays a dialog
 * @param {HTMLDialogElement} dialog Element reference
 */
export function showDialog(dialog) {
    dialog.showModal();
}

/**
 * Close a dialog
 * @param {HTMLDialogElement} dialog Element reference
 * @param {string?} returnValue Value to set for
 */
export function closeDialog(dialog, returnValue) {
    dialog.close(returnValue);
}