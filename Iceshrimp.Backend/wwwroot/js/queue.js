function filter(queue) {
    const f = document.getElementById('filter').value;
    if (f === 'all') {
        window.location.href = `/queue/${queue}`;
    } else {
        window.location.href = `/queue/${queue}/1/${f}`;
    }
}

function lookupJob(e) {
    e.preventDefault();
    window.location.href = `/queue/job/${document.getElementById('lookup').value}`;
    return false;
}

function navigate(event) {
    const target = event.target.getAttribute('data-target')
    if (event.ctrlKey || event.metaKey)
        window.open(target, '_blank');
    else
        window.location.href = target;
}

async function copyToClipboard(text) {
    await navigator.clipboard.writeText(text);
}

async function copyElementToClipboard(id) {
    await copyToClipboard(document.getElementById(id).textContent);
}

function getCookie(key) {
    let result;
    return (result = new RegExp('(?:^|; )' + encodeURIComponent(key) + '=([^;]*)').exec(document.cookie)) ? (result[1]) : null;
}

async function callApiMethod(route) {
    const cookie = getCookie('admin_session');
    if (cookie == null) throw new Error('Failed to get admin_session cookie');
    return await fetch(route, {
        method: 'POST',
        headers: {
            'Authorization': `Bearer ${cookie}`
        }
    });
}

async function retry(id) {
    await callApiMethod(`/api/iceshrimp/admin/queue/jobs/${id}/retry`);
    window.location.reload();
}

async function retryAllFailed(queue) {
    await callApiMethod(`/api/iceshrimp/admin/queue/${queue}/retry-all`);
    window.location.reload();
}

async function retryAllOnPage(queue) {
    const elements = document.getElementsByClassName("uuid");
    const items = Array.prototype.map.call(elements, x => x.textContent).sort();
    const first = items[0];
    const last = items[items.length - 1];
    await callApiMethod(`/api/iceshrimp/admin/queue/${queue}/retry-range/${first}/${last}`);
    window.location.reload();
}

async function abandon(id) {
    await callApiMethod(`/api/iceshrimp/admin/queue/jobs/${id}/abandon`);
    window.location.reload();
}