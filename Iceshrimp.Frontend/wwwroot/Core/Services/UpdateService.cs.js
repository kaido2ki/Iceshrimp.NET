const broadcast = new BroadcastChannel('update-channel');

export async function RegisterSWUpdateCallback(dotNetHelper){
    broadcast.onmessage = (event) => {
        if (event.data && event.data.type === 'INSTALLING_WORKER') {
            dotNetHelper.invokeMethod('NewServiceWorker');
        }
    };
}

export async function ServiceWorkerCheckRegistration(){
    if (navigator.serviceWorker == null) return null;
    const registration = await navigator.serviceWorker.getRegistration();
    if (registration.installing) return "installing";
    if (registration.waiting) return "waiting";
    if (registration.active) return "active";
}

export async function ServiceWorkerUpdate(){
    if (navigator.serviceWorker == null) return null;
    const registration = await navigator.serviceWorker.getRegistration();
    var res = await registration.update();
    if (res.installing) return "installing";
    if (res.waiting) return "waiting";
    if (res.active) return "active";return !!(res.installing || res.waiting);
}

export async function ServiceWorkerSkipWaiting(){
    if (navigator.serviceWorker == null) return null;
    const registration = await navigator.serviceWorker.getRegistration();
    if (registration.waiting){
        registration.waiting.postMessage({ type: 'SKIP_WAITING' })
        return true;
    }
    else {
        return false;
    }
}
