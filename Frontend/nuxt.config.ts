
export default defineNuxtConfig({
  compatibilityDate: '2024-12-20',
  devtools: { enabled: true },
  typescript: { strict: true },
  // Read API base from environment so builds can target the correct backend.
  // Priority: NUXT_PUBLIC_API_BASE (preferred) -> DOMAIN (fallback) -> empty (use same origin)
  runtimeConfig: { public: { apiBase: process.env.NUXT_PUBLIC_API_BASE || process.env.DOMAIN || "" } },
  modules: ['@pinia/nuxt'],
  app: { head: { title: 'FACET', meta: [{ name: 'viewport', content: 'width=device-width, initial-scale=1' }], link: [{ rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }] } },
  css: ['@/assets/css/tailwind.css'],
  postcss: { plugins: { tailwindcss: {}, autoprefixer: {} } }
})  