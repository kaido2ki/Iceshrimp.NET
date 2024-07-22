let interval = null;
const timeout = 1000;
let initial = true;

async function reloadTables() {
    if (document.hidden) {
        setStatus("Disconnected", "status-failed");
        if (interval == null) return;
        clearInterval(interval);
        interval = null;
        return;
    }

    if (interval == null && !initial) setStatus("Reconnecting...", "status-delayed");
    if (initial) initial = false;
    interval ??= setInterval(reloadTables, timeout);

    try {
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
        setStatus("Updating in real time", "status-completed");
    }
    catch {
        setStatus("Reconnecting...", "status-delayed");
    }
}

function setStatus(text, classname) {
    const el = document.getElementById('update-status');
    el.innerText = text;
    el.className = classname;
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