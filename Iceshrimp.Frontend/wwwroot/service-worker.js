// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development more difficult (changes would not
// be reflected on the first load after each change).
self.addEventListener('fetch', () => { });
self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting().then(() => event.source.postMessage({type: "REQUEST_RELOAD"}));
    }
});

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

    event.waitUntil(
        self.registration.showNotification(payload.notifierName ?? payload.instanceName, options)
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
