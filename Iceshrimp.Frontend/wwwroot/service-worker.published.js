// Caution! Be sure you understand the caveats before publishing an application with
// offline support. See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');
self.addEventListener('install', event => event.waitUntil(onInstall(event)));
self.addEventListener('activate', event => event.waitUntil(onActivate(event)));
self.addEventListener('fetch', event => {
    if (!shouldFetch(event.request.url)) return;
    event.respondWith(onFetch(event));
});

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;
const offlineAssetsInclude = [/\.dll$/, /\.pdb$/, /\.wasm/, /\.html/, /\.js$/, /\.json$/, /\.css$/, /\.woff$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/];
const offlineAssetsExclude = [/^service-worker\.js$/];

// Replace with your base path if you are hosting on a subfolder. Ensure there is a trailing '/'.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(asset => new URL(asset.url, baseUrl).href);

async function onInstall(event) {
    console.info('Service worker: Install');

    // Fetch and cache all matching items from the assets manifest
    const assetsRequests = self.assetsManifest.assets
        .filter(asset => shouldFetch(asset.url))
        .map(asset => new Request(asset.url, {integrity: asset.hash, cache: 'no-cache'}));
    await caches.open(cacheName).then(cache => cache.addAll(assetsRequests));
}

async function onActivate(event) {
    console.info('Service worker: Activate');

    // Delete unused caches
    const cacheKeys = await caches.keys();
    await Promise.all(cacheKeys
        .filter(key => key.startsWith(cacheNamePrefix) && key !== cacheName)
        .map(key => caches.delete(key)));
}

/** @param {url} string */
function shouldFetch(url) {
    if (offlineAssetsInclude.some(pattern => !pattern.test(url))) return false;
    if (offlineAssetsExclude.some(pattern => pattern.test(url))) return false;
    
    return true;
}

async function onFetch(event) {
    let cachedResponse = null;
    if (event.request.method === 'GET') {
        // For all navigation requests, try to serve index.html from cache,
        // unless that request is for an offline resource.
        // If you need some URLs to be server-rendered, edit the following check to exclude those URLs
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.some(url => url === event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;
        const cache = await caches.open(cacheName);
        cachedResponse = await cache.match(request);
    }

    return cachedResponse;
}

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
