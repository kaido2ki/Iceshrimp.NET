/**
 * Displays a dialog
 * @param {HTMLDialogElement} dialog Element reference
 * @param {boolean} modal Show as a modal
 */
export function showDialog(dialog, modal) {
    if (modal) dialog.showModal();
    else dialog.show();
}

/**
 * Close a dialog
 * @param {HTMLDialogElement} dialog Element reference
 * @param {string?} returnValue Value to set for
 */
export function closeDialog(dialog, returnValue) {
    dialog.close(returnValue);
}

/**
 * Scroll to element
 * @param {HTMLElement} element
 * @param {"auto" | "instant" | "smooth"} behavior
 */
export function scrollToElement(element, behavior) {
    element.scrollIntoView({behavior});
}

/**
 * Simulates a mouse click on an element
 * @param {HTMLElement} element
 */
export function clickElement(element) {
    element.click();
}

/**
 * Gets the position of an element
 * @param {HTMLElement} element Element
 * @param {boolean} includeScroll Include window scroll in position
 * @return {number[]} Element X and Y coordinates
 */
export function getPosition(element, includeScroll) {
    const rect = element.getBoundingClientRect();
    let x = rect.x + (includeScroll ? window.scrollX : 0);
    let y = rect.y + (includeScroll ? window.scrollY : 0);
    return [Math.round(x), Math.round(y)];
}

/**
 * Get start of a selection
 * @param element Element
 * @return {number} Start index
 */
export function getSelectionStart(element) {
    return element.selectionStart;
}