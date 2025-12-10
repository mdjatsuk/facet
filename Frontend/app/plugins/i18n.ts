import { createI18n } from 'vue-i18n'
import en from '../locales/en.json'
import ru from '../locales/ru.json'
import est from '../locales/est.json'

export default defineNuxtPlugin(({ vueApp }) => {
  let savedLocale = 'en'
  
  // Only access localStorage in browser
  if (process.client) {
    savedLocale = localStorage.getItem('language') || 'en'
  }

  const i18n = createI18n({
    legacy: false,
    locale: savedLocale,
    fallbackLocale: 'en',
    messages: {
      en,
      ru,
      est
    }
  })

  vueApp.use(i18n)
})
