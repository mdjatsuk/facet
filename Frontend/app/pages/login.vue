<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

const { locale, t } = useI18n()
const showLanguages = ref(false)

const languages = [
  { code: 'en', short: 'Eng', name: 'English' },
  { code: 'ru', short: 'Рус', name: 'Русский' },
  { code: 'est', short: 'Est', name: 'Eesti' }
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
    <div class="fixed top-4 right-4 z-50">
      <button
        @click="showLanguages = !showLanguages"
        class="flex items-center gap-2 px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition"
      >
        <span class="text-sm">{{ languages.find(l => l.code === locale)?.short || 'EN' }}</span>
        <svg class="w-4 h-4 transition-transform" :class="{ 'rotate-180': showLanguages }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 14l-7 7m0 0l-7-7m7 7V3" />
        </svg>
      </button>
      
      <!-- Language Dropdown -->
      <div v-if="showLanguages" class="absolute top-full right-0 mt-2 bg-white border border-slate-200 rounded-lg shadow-lg z-50 min-w-48">
        <div class="py-2">
          <button
            v-for="lang in languages"
            :key="lang.code"
            @click="changeLanguage(lang.code)"
            class="w-full flex items-center gap-3 px-4 py-2 hover:bg-slate-100 transition-colors text-left"
            :class="{ 'bg-primary/10 border-r-2 border-primary': lang.code === locale }"
          >
            <span class="font-semibold text-slate-700">{{ lang.short }}</span>
            <div>
              <div class="font-medium text-slate-700">{{ lang.name }}</div>
            </div>
            <svg v-if="lang.code === locale" class="w-5 h-5 text-blue-600 ml-auto" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
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
