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
    console.debug("Added to observer: ", element);
}

/**
 * Remove an entry from the intersection observer
 * @param {Element} element
 */
export function removeEntry(element) {
    observer.unobserve(element);
    console.debug(`Removed ${element} from observer`);
}

/**
 * Observer callback
 * @param {IntersectionObserverEntry[]} entries
 * @param {IntersectionObserver} observer
 */
function observe(entries, observer) {
    for (const entry of entries) {
        const id = entry.target.getAttributeNames().find(a => a.startsWith("_bl_")).substring(4);
        console.debug(`Observed ${id}`);
        dotnet.invokeMethod("Observe", id, entry);
    }
}