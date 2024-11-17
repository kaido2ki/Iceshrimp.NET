export async function RegisterUpdateCallback(dotNetHelper) {
    const registration = await navigator.serviceWorker.getRegistration();
    if (registration) {
        registration.addEventListener("updatefound", () => {
            dotNetHelper.invokeMethodAsync('OnUpdateFound');
        })
    }
    else {
        console.error("Failed to get service worker registration")
    }
}

export async function ServiceWorkerCheckWaiting(){
    const registration = await navigator.serviceWorker.getRegistration();
    return registration.waiting ? true : false;
}

export async function ServiceWorkerUpdate(){
    const registration = await navigator.serviceWorker.getRegistration();
    await registration.update();
}

export async function ServiceWorkerSkipWaiting(){
    const registration = await navigator.serviceWorker.getRegistration();
    if (registration.waiting){
        registration.waiting.postMessage({ type: 'SKIP_WAITING' })
        return true;
    }
    else {
        return false;
    }
}