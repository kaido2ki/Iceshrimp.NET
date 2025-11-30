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
    if (!registration) return null;
    if (registration.installing) return "installing";
    if (registration.waiting) return "waiting";
    if (registration.active) return "active";
    else return null
}

export async function ServiceWorkerUpdate(){
    if (navigator.serviceWorker == null) return null;
    const registration = await navigator.serviceWorker.getRegistration();
    if (!registration) return null;
    var res = await registration.update();
    if (res.installing) return "installing";
    if (res.waiting) return "waiting";
    if (res.active) return "active";
    else return null;
}

export async function ServiceWorkerSkipWaiting(){
    if (navigator.serviceWorker == null) return null;
    const registration = await navigator.serviceWorker.getRegistration();
    if (registration?.waiting){
        registration.waiting.postMessage({ type: 'SKIP_WAITING' })
        return true;
    }
    else {
        return false;
    }
}

export async function UnregisterAll(){
    let regs = await navigator.serviceWorker.getRegistrations();
    for (let i of regs) {
        await i.unregister();
    }
    let newRegs = await navigator.serviceWorker.getRegistrations();
    return newRegs.length <= 0;
}
