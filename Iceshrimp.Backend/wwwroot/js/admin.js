function navigate(event) {
    const target = event.target.getAttribute('data-target')
    if (event.ctrlKey || event.metaKey)
        window.open(target, '_blank');
    else
        window.location.href = target;
}

function getCookie(key) {
    let result;
    return (result = new RegExp('(?:^|; )' + encodeURIComponent(key) + '=([^;]*)').exec(document.cookie)) ? (result[1]) : null;
}

async function confirm(target, action) {
    const match = " (confirm)";
    if (!target.innerText.endsWith(match)) {
        target.innerText += match;
    }
    else {
        await action();
        window.location.reload();
    }
}

async function unblockInstance(host, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/instances/${host}/unblock`));
}

async function disallowInstance(host, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/instances/${host}/disallow`));
}

async function downloadFile(blob, filename) {
    let url = URL.createObjectURL(blob);

    let a = document.createElement('a');
    a.download = filename;
    a.href = url;
    a.style.display = "none";
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);

    setTimeout(() => { URL.revokeObjectURL(url) });
}

async function exportBlocklist() {
    let blocks = await callApiMethod("/api/iceshrimp/admin/instances/blocked", "GET").then(res => res.blob());
    await downloadFile(blocks, "blocklist.json");
}

async function exportAllowlist() {
    let allows = await callApiMethod("/api/iceshrimp/admin/instances/allowed", "GET").then(res => res.blob());
    await downloadFile(allows, "allowlist.json");
}

async function importBlocklist(ev) {
    ev.preventDefault()
    let input = document.getElementById('blocklist-import-file');
    let file = input.files[0];
    if (file == null) return;
    let data = new FormData();
    data.append('file', input.files[0]);
    await callApiMethod("/api/iceshrimp/admin/instances/blocked/import", "POST", data);
    window.location.reload();
}

async function importAllowlist(ev) {
    ev.preventDefault()
    let input = document.getElementById('allowlist-import-file');
    let file = input.files[0];
    if (file == null) return;
    let data = new FormData();
    data.append('file', input.files[0]);
    await callApiMethod("/api/iceshrimp/admin/instances/allowed/import", "POST", data);
    window.location.reload();
}

async function debubbleInstance(host, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/instances/${host}/debubble`));
}

async function revokeInvite(code, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/invites/${code}/revoke`));
}

async function removeRelay(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/relays/${id}`, 'DELETE'));
}

async function removeRule(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/instance/rules/${id}`, 'DELETE'));
}

async function suspendUser(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/moderation/users/${id}/suspend`));
}

async function unsuspendUser(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/moderation/users/${id}/unsuspend`));
}

async function deleteUser(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/moderation/users/${id}/delete`));
}

async function purgeUser(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/moderation/users/${id}/purge`));
}

async function runCronTask(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/tasks/${id}/run`));
}

async function generateInvite() {
    const res = await callApiMethod(`/api/iceshrimp/admin/invites/generate`);
    const json = await res.json();
    return json.code;
}

async function generateInviteAndCopy() {
    const invite = await generateInvite();
    await copyToClipboard(invite);
    const elem = document.getElementById("gen-invite");
    const old = elem.innerText;
    elem.innerText += " (copied!)";
    elem.role = "alert";
    setTimeout(() => {
        elem.role = "button";
        elem.innerText = old;
    }, 2500);
}

async function callApiMethod(route, method, data) {
    const cookie = getCookie('admin_session');
    if (cookie == null) throw new Error('Failed to get admin_session cookie');
    return await fetch(route, {
        method: method ?? 'POST',
        body: data,
        headers: {
            'Authorization': `Bearer ${cookie}`
        }
    });
}

async function copyToClipboard(text) {
    await navigator.clipboard.writeText(text);
}

async function copyElementToClipboard(id) {
    await copyToClipboard(document.getElementById(id).textContent);
}