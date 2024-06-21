function filter(queue) {
    const f = document.getElementById('filter').value;
    if (f === 'all') {
        window.location.href = `/queue/${queue}`;
    }
    else {
        window.location.href = `/queue/${queue}/1/${f}`;
    }
}

function lookupJob(e) {
    e.preventDefault();
    window.location.href = `/queue/job/${document.getElementById('lookup').value}`;
    return false;
}

function navigate(target) {
    window.location.href = target;
}

async function copyToClipboard(text) {
    await navigator.clipboard.writeText(text);
}

async function copyElementToClipboard(id) {
    await copyToClipboard(document.getElementById(id).textContent);
}