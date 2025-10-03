import type { Config } from 'tailwindcss'
export default {
  content: ['./app.vue','./pages/**/*.{vue,js,ts}','./components/**/*.{vue,js,ts}','./composables/**/*.{js,ts}','./src/**/*.{vue,js,ts}'],
  theme: { extend: { colors: { primary: '#0ea5e9', ink: '#0b1220' }, boxShadow: { soft: '0 10px 25px -10px rgba(0,0,0,0.15)' } } },
  plugins: []
} satisfies Config
