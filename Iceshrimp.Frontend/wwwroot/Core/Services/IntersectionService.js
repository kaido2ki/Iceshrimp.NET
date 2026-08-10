/**
 * @type IntersectionObserver
 */
let observer;
let dotnet;

export function setupObserver(dotNetObjectReference) {
    observer = new IntersectionObserver(observe);
    dotnet = dotNetObjectReference;
    console.debug("Initialized observer");
}

/**
 * Add an entry to the intersection observer
 * @param {Element} element
 */
export function addEntry(element) {
    observer.observe(element);
}

/**
 * Remove an entry from the intersection observer
 * @param {Element} element
 */
export function removeEntry(element) {
    observer.unobserve(element);
}

/**
 * Observer callback
 * @param {IntersectionObserverEntry[]} entries
 * @param {IntersectionObserver} observer
 */
function observe(entries, observer) {
    for (const entry of entries) {
        // Blazor adds a valueless _bl_{id} attribute to elements with a @ref which matches ElementReference.Id
        const id = entry.target.getAttributeNames().find(a => a.startsWith("_bl_")).substring(4);

        dotnet.invokeMethodAsync("Observe", id, entry.intersectionRatio, entry.isIntersecting);
    }
}