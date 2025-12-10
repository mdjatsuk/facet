<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

const { locale, t } = useI18n()
const showLanguages = ref(false)

const languages = [
  { code: 'en', name: 'Eng', flag: 'Eng' },
  { code: 'ru', name: 'Rus', flag: 'Rus' },
  { code: 'est', name: 'Est', flag: 'Est' }
]

function changeLanguage(code: string) {
  locale.value = code
  if (process.client) {
    localStorage.setItem('language', code)
  }
  showLanguages.value = false
}
</script>

<template>
  <div class="min-h-screen bg-slate-50 flex items-center justify-center px-4 sm:px-6 relative">
    <!-- Language Selector Button -->
    <div class="fixed top-4 right-4">
      <button
        @click="showLanguages = !showLanguages"
        class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium text-sm transition"
      >
        🌐 {{ languages.find(l => l.code === locale)?.name || 'Language' }}
      </button>
      
      <!-- Language Dropdown -->
      <div v-if="showLanguages" class="absolute top-12 right-0 bg-white border border-slate-200 rounded-lg shadow-lg z-50 min-w-40">
        <button
          v-for="lang in languages"
          :key="lang.code"
          @click="changeLanguage(lang.code)"
          :class="[
            'w-full text-left px-4 py-3 flex items-center gap-2 hover:bg-slate-100 transition',
            locale === lang.code ? 'bg-blue-50 font-medium text-blue-700' : ''
          ]"
        >
          <span>{{ lang.flag }}</span>
          <span>{{ lang.name }}</span>
        </button>
      </div>
    </div>

    <div class="max-w-2xl w-full">
      <div class="mb-6 sm:mb-8 text-center">
        <h1 class="text-3xl sm:text-4xl font-semibold">{{ $t('login.title') }}</h1>
        <p class="mt-2 sm:mt-3 text-sm sm:text-base text-slate-600">{{ $t('login.description') }}</p>
      </div>

      <ClientOnly>
        <LoginForm />
      </ClientOnly>
      
      <div class="text-center mt-4">
        <a href="/register" class="text-sm text-slate-600 underline">{{ $t('login.createAccount') }}</a>
      </div>
    </div>
  </div>
</template>
