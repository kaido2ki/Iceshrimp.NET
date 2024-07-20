async function reloadTables() {
    const last = document.getElementById('last-updated').innerText;

    fetch(`/queue?last=${last}`).then(res => {
        res.text().then(text => {
            const newDocument = new DOMParser().parseFromString(text, "text/html");
            const newLast = newDocument.getElementById('last-updated').innerText;
            if (last !== newLast) {
                document.getElementById('last-updated').innerText = newLast;
                document.getElementById('queue-status').innerHTML = newDocument.getElementById('queue-status').innerHTML;
                document.getElementById('recent-jobs').innerHTML = newDocument.getElementById('recent-jobs').innerHTML;
            }
        })
    });
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

docReady(() => setInterval(reloadTables, 2000));