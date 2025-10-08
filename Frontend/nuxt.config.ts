
export default defineNuxtConfig({
  compatibilityDate: '2024-12-20',
  devtools: { enabled: true },
  typescript: { strict: true },
  runtimeConfig: { public: { apiBase: "https://localhost:7072/" } },
  app: { head: { title: 'FACET', meta: [{ name: 'viewport', content: 'width=device-width, initial-scale=1' }], link: [{ rel: 'icon', type: 'image/svg+xml', href: '/favicon.svg' }] } },
  css: ['@/assets/css/tailwind.css'],
  postcss: { plugins: { tailwindcss: {}, autoprefixer: {} } }
})  