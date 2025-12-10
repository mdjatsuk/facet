import { useI18n } from 'vue-i18n'
import { computed } from 'vue'

export function useLanguage() {
  const { locale } = useI18n()
  const languages = [
    { code: 'en', name: 'English', flag: '🇬🇧' },
    { code: 'ru', name: 'Русский', flag: '🇷🇺' },
    { code: 'est', name: 'Eesti', flag: '🇪🇪' }
  ]

  const currentLanguage = computed(() => {
    return languages.find(l => l.code === locale.value) || languages[0]
  })

  const setLanguage = (code: string) => {
    locale.value = code
    if (process.client) {
      localStorage.setItem('language', code)
      document.documentElement.lang = code
    }
  }

  return {
    languages,
    currentLanguage,
    setLanguage
  }
}
