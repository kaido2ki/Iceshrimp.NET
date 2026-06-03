/**
 * Setup push notifications
 * @param {string} vapidKey Server's VAPID public key
 * @returns {Promise<PushSubscriptionJSON | null>} Push subscription object
 */
export async function setupPush(vapidKey) {
    /** @type {PushSubscriptionOptionsInit} */
    const pushSubscriptionOptions = {applicationServerKey: vapidKey, userVisibleOnly: true};

    const registration = await navigator.serviceWorker.ready;
    const permission = await registration.pushManager.permissionState(pushSubscriptionOptions);

    if (permission === "denied") {
        console.warn("Notification permission is denied. Push notifications will not work.")
        return null;
    } else if (permission === "prompt") {
        const permission = await Notification.requestPermission();
        if (permission !== "granted") return null;
    }

    const subscription = await registration.pushManager.subscribe(pushSubscriptionOptions);

    if (!subscription) {
        console.error("Failed to subscribe to push notifications");
        return null;
    }

    return subscription.toJSON();
}