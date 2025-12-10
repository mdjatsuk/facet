<template>
  <div class="relative">
    <button
      @click="open = !open"
      class="flex items-center gap-2 px-3 py-2 rounded-lg hover:bg-slate-100 transition-colors"
      :aria-label="`Language: ${currentLanguage?.name || 'EN'}`"
    >
      <span class="text-sm font-medium text-slate-700">{{ currentLanguage?.short || 'EN' }}</span>
      <svg class="w-4 h-4 text-slate-600 transition-transform" :class="{ 'rotate-180': open }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 14l-7 7m0 0l-7-7m7 7V3" />
      </svg>
    </button>

    <!-- Dropdown Menu -->
    <transition name="fade">
      <div v-if="open" class="fixed inset-0 z-30" @click="open = false" />
    </transition>

    <transition name="slide-up">
      <div
        v-if="open"
        class="absolute top-full right-0 mt-2 bg-white rounded-lg shadow-lg border border-slate-200 z-40 min-w-48"
      >
        <div class="py-2">
          <button
            v-for="lang in languages"
            :key="lang.code"
            @click="selectLanguage(lang.code)"
            class="w-full flex items-center gap-3 px-4 py-2 hover:bg-slate-100 transition-colors text-left"
            :class="{ 'bg-primary/10 border-r-2 border-primary': lang.code === currentLanguage?.code }"
          >
            <div class="text-sm font-medium text-slate-700 min-w-6">{{ lang.short }}</div>
            <div>
              <div class="font-medium text-slate-700">{{ lang.name }}</div>
            </div>
            <svg v-if="lang.code === currentLanguage?.code" class="w-5 h-5 text-primary ml-auto" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </div>
    </transition>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useI18n } from 'vue-i18n'

const open = ref(false)
const { locale } = useI18n()

const languages = [
  { code: 'en', name: 'English', short: 'Eng' },
  { code: 'ru', name: 'Русский', short: 'Рус' },
  { code: 'est', name: 'Eesti', short: 'Est' }
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

const selectLanguage = (code: string) => {
  setLanguage(code)
  open.value = false
}
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.2s;
}

.slide-up-enter-from,
.slide-up-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
</style>
