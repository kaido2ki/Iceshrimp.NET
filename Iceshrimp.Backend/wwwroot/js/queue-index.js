let interval = null;
const timeout = 1000;

async function reloadTables() {
    if (document.hidden) {
        if (interval == null) return;
        clearInterval(interval);
        interval = null;
        return;
    }

    interval ??= setInterval(reloadTables, timeout);

    const last = document.getElementById('last-updated').innerText;
    const res = await fetch(`/queue?last=${last}`);
    const text = await res.text();

    const newDocument = new DOMParser().parseFromString(text, "text/html");
    const newLast = newDocument.getElementById('last-updated').innerText;

    if (last !== newLast) {
        document.getElementById('last-updated').innerText = newLast;
        document.getElementById('recent-jobs').innerHTML = newDocument.getElementById('recent-jobs').innerHTML;
    }
    document.getElementById('queue-status').innerHTML = newDocument.getElementById('queue-status').innerHTML;
}

function docReady(fn) {
    // see if DOM is already available
    if (document.readyState === "complete" || document.readyState === "interactive") {
        // call on next available tick
        setTimeout(fn, 1);
    } else {
        document.addEventListener("DOMContentLoaded", fn);
    }
}

docReady(async () => {
    document.addEventListener("visibilitychange", reloadTables, false);
    await reloadTables();
});