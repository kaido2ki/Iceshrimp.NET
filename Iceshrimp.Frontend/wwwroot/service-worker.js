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
    let options = {tag: payload.id};
    if ("iconUrl" in payload) options.icon = payload.iconUrl;

    if (payload.notifierId && payload.notifierName && payload.notifierUsername) {
        options.navigate = `/@${payload.notifierUsername}`;
        if (payload.noteId) options.navigate = `/notes/${payload.noteId}`;
        if (payload.reportId) options.navigate = `/mod/reports/${payload.reportId}`;

        // TODO: hook this into localization instead of hardcoding the bodies
        if (payload.type === "Follow") options.body = "followed you";
        else if (payload.type === "Mention") options.body = "mentioned you";
        else if (payload.type === "Reply") options.body = "replied to your note";
        else if (payload.type === "Renote") options.body = "renoted your note";
        else if (payload.type === "Renote") options.body = "quoted your note";
        else if (payload.type === "Like") options.body = "liked your note";
        else if (payload.type === "Reaction" && payload.reaction) options.body = `reacted ${payload.reaction} to your note`;
        else if (payload.type === "PollVote") options.body = "voted in your poll";
        else if (payload.type === "PollEnded") options.body = "poll has ended";
        else if (payload.type === "FollowRequestReceived") options.body = "requested to follow you";
        else if (payload.type === "FollowRequestAccepted") options.body = "accepted your follow request";
        else if (payload.type === "Edit") options.body = "edited a note";
        else if (payload.type === "Bite") options.body = "bit you";
        else if (payload.type === "Report") options.body = "reported a user";
    } else if (!options.body) {
        console.warn(`Received unknown notification ${payload.id}`);
        return;
    }

    event.waitUntil(
        self.registration.showNotification(payload.notifierName ?? payload.instanceName, options)
    );
});
