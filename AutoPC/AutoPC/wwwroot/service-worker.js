// ARIA v3.0 Service Worker — PWA Offline Support
const CACHE_NAME = 'aria-v3-cache-v1';
const OFFLINE_URL = '/offline.html';

// Assets to pre-cache for offline shell
const PRECACHE_ASSETS = [
  '/',
  '/chat',
  '/settings',
  '/manifest.json',
  '/app.css',
  '/wandavision-effects.css',
  '/icons/aria-192.svg',
  '/icons/aria-512.svg',
  '/offline.html'
];

// Install: pre-cache the app shell
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(CACHE_NAME).then(cache => {
      return cache.addAll(PRECACHE_ASSETS).catch(err => {
        console.warn('[SW] Some assets failed to pre-cache:', err);
      });
    })
  );
  self.skipWaiting();
});

// Activate: clean up old caches
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys().then(keys =>
      Promise.all(
        keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))
      )
    )
  );
  self.clients.claim();
});

// Fetch: network-first for API calls, cache-first for static assets
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);

  // Never cache API calls or Ollama proxy requests
  if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/_framework/')) {
    event.respondWith(
      fetch(event.request).catch(() => {
        // If it's a navigation request and we're offline, show offline page
        if (event.request.mode === 'navigate') {
          return caches.match(OFFLINE_URL);
        }
        return new Response('{"error":"offline"}', {
          status: 503,
          headers: { 'Content-Type': 'application/json' }
        });
      })
    );
    return;
  }

  // Cache-first for static assets
  event.respondWith(
    caches.match(event.request).then(cached => {
      if (cached) return cached;

      return fetch(event.request).then(response => {
        // Cache successful GET responses for static assets
        if (response.ok && event.request.method === 'GET') {
          const responseClone = response.clone();
          caches.open(CACHE_NAME).then(cache => {
            cache.put(event.request, responseClone);
          });
        }
        return response;
      }).catch(() => {
        if (event.request.mode === 'navigate') {
          return caches.match(OFFLINE_URL);
        }
        return new Response('Offline', { status: 503 });
      });
    })
  );
});
