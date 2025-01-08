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

async function removeRelay(id, target) {
    await confirm(target, () => callApiMethod(`/api/iceshrimp/admin/relays/${id}`, 'DELETE'));
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

async function callApiMethod(route, method) {
    const cookie = getCookie('admin_session');
    if (cookie == null) throw new Error('Failed to get admin_session cookie');
    return await fetch(route, {
        method: method ?? 'POST',
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