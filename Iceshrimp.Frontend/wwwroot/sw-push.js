/**
 * Is the client currently focused
 * @returns {boolean}
 */
function isClientFocused() {
    return clients
        .matchAll({
            type: 'window',
            includeUncontrolled: true,
        })
        .then((windowClients) => {
            for (const client of windowClients) {
                if (client.focused) {
                    return true;
                }
            }
            return false;
        });
}

self.addEventListener('push', (event) => {
    const payload = event.data.json();
    if (!("type" in payload && "id" in payload)) return;

    /** @type {NotificationOptions} */
    let options = {data: {}, tag: payload.id};
    if ("iconUrl" in payload) options.icon = payload.iconUrl;

    if (payload.notifierId && payload.notifierName && payload.notifierUsername) {
        options.data.url = `/@${payload.notifierUsername}`;
        if (payload.noteId) options.data.url = `/notes/${payload.noteId}`;
        if (payload.reportId) options.data.url = `/mod/reports/${payload.reportId}`;
        options.navigate = options.data.url;

        // TODO: hook this into localization instead of hardcoding the bodies
        if (payload.type === "follow") options.body = "followed you";
        else if (payload.type === "mention") options.body = "mentioned you";
        else if (payload.type === "reply") options.body = "replied to your note";
        else if (payload.type === "renote") options.body = "renoted your note";
        else if (payload.type === "quote") options.body = "quoted your note";
        else if (payload.type === "like") options.body = "liked your note";
        else if (payload.type === "reaction" && payload.reaction) options.body = `reacted ${payload.reaction} to your note`;
        else if (payload.type === "poll_vote") options.body = "voted in your poll";
        else if (payload.type === "poll_ended") options.body = "poll has ended";
        else if (payload.type === "follow_request_received") options.body = "requested to follow you";
        else if (payload.type === "follow_request_accepted") options.body = "accepted your follow request";
        else if (payload.type === "edit") options.body = "edited a note";
        else if (payload.type === "bite") options.body = "bit you";
        else if (payload.type === "report") options.body = "reported a user";
    }

    options.body ??= `unknown notification type ${payload.id}`;

    if (payload.notePreview) options.body += `: "${payload.notePreview}"`;

    event.waitUntil(
        isClientFocused().then((focused) => {
            if (!focused) {
                return self.registration.showNotification(payload.notifierName ?? payload.instanceName, options);
            }
        })
    );
});

self.addEventListener("notificationclick", (event) => {
    event.notification.close();

    if (!event.notification.data.url) return;

    event.waitUntil(
        clients
            .matchAll({
                type: "window",
            })
            .then((clientList) => {
                for (const client of clientList) {
                    if (client.url === event.notification.data.url && "focus" in client) return client.focus();
                }
                if (clients.openWindow) return clients.openWindow(event.notification.data.url);
            }),
    );
})