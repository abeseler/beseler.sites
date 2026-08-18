// Installability only. Blazor Server cannot work offline, so this worker
// never caches and never intercepts requests.
self.addEventListener("install", (event) => {
    event.waitUntil(self.skipWaiting());
});

self.addEventListener("activate", (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", () => {
    // Present so Chromium treats the app as installable.
    // No respondWith — the browser talks to the network as usual.
});
