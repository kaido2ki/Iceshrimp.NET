navigator.serviceWorker.addEventListener("message", (event) => {
    if (event.data && event.data.type === 'REQUEST_RELOAD') {
        window.location.reload(true);
    }
});

let checkCount = 0;
let checkInterval = null;
let dotnet = null;

export async function startSwUpdateChecking(dotnetRef) {
    dotnet = dotnetRef;
    checkCount = 0;
    checkInterval = window.setInterval(checkSwUpdates, 3000);
}

async function checkSwUpdates() {
    const reg = await navigator.serviceWorker.getRegistration();
    console.info("Checking service worker for updates");

    if (reg.waiting) {
        dotnet.invokeMethod("UpdateReady");
        window.clearInterval(checkInterval);
    } else if (!reg.installing && checkCount >= 5) {
        dotnet.invokeMethod("NoUpdate");
        window.clearInterval(checkInterval);
    }

    checkCount++;
}

export async function swSkipWaiting() {
    const reg = await navigator.serviceWorker.getRegistration();

    if (reg.waiting) {
        reg.waiting.postMessage({type: "SKIP_WAITING"});
    }
}

export async function unregisterAllSw() {
    const regs = await navigator.serviceWorker.getRegistrations();

    for (let reg of regs) {
        await reg.unregister();
    }
}