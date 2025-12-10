
const rawApiBase = process.env.NUXT_PUBLIC_API_BASE || process.env.DOMAIN || ''
const apiBase = rawApiBase && !/^https?:\/\//i.test(rawApiBase) ? `https://${rawApiBase}` : rawApiBase

export default defineNuxtConfig({
  compatibilityDate: '2024-12-20',
  devtools: { enabled: true },
  typescript: { strict: true },
  // Read API base from environment so builds can target the correct backend.
  // Priority: NUXT_PUBLIC_API_BASE (preferred) -> DOMAIN (fallback) -> empty (use same origin)
  // Normalize so the value always is either empty or an absolute URL starting with http(s)
  runtimeConfig: { public: { apiBase } },
  modules: ['@pinia/nuxt'],
    nitro: {
      compressPublicAssets: true,
      routeRules: {
        '/**': { 
          headers: { 
            'Cache-Control': 'no-cache, no-store, must-revalidate',
            'X-Content-Type-Options': 'nosniff',
            'X-Frame-Options': 'DENY'
          } 
        }
      }
    },
  app: { 
    head: { 
      title: 'FACET', 
      meta: [
        { name: 'viewport', content: 'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes, viewport-fit=cover' },
        // Strengthen mobile detection for older mobile browsers
        { name: 'HandheldFriendly', content: 'true' },
        { name: 'MobileOptimized', content: 'width' },
        { name: 'apple-mobile-web-app-capable', content: 'yes' },
        { name: 'apple-mobile-web-app-status-bar-style', content: 'black-translucent' },
        { name: 'apple-mobile-web-app-title', content: 'FACET' },
        { name: 'format-detection', content: 'telephone=no' },
        { name: 'mobile-web-app-capable', content: 'yes' },
        { name: 'theme-color', content: '#0ea5e9' }
      ], 
      link: [
        { rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' },
        { rel: 'apple-touch-icon', href: '/favicon.svg' }
      ],
      htmlAttrs: {
        lang: 'en'
      },
      bodyAttrs: {
        class: 'antialiased'
      }
    } 
  },
  css: ['@/assets/css/tailwind.css'],
  postcss: { plugins: { tailwindcss: {}, autoprefixer: {} } }
})  