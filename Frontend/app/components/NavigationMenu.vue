<template>
  <nav class="bg-white border-b border-slate-100 shadow-sm p-3">
      <div class="flex items-center justify-between">
        <button class="p-2 rounded-md border border-slate-200" @click="open = !open" aria-label="Menu">
          <!-- Modern hamburger icon (3 lines, rounded ends) -->
          <svg class="w-6 h-6 text-slate-700" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M3.75 6.75h16.5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
            <path d="M3.75 12h16.5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
            <path d="M3.75 17.25h16.5" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"/>
          </svg>
        </button>
        <!-- Inline FACET logo + title on mobile top bar -->
        <div class="flex items-center gap-2">
          <img src="/favicon.svg" alt="FACET" class="h-6 w-6" />
          <span class="text-base font-semibold text-ink">FACET</span>
        </div>
      </div>
      <!-- Removed top-right LanguageSwitcher to avoid duplicate when menu opens -->
    <!-- Overlay -->
    <transition name="fade">
      <div v-if="open" class="fixed inset-0 bg-black/20 z-40" @click="open = false"></div>
    </transition>

    <!-- Drawer -->
    <transition name="slide">
      <aside v-if="open" class="fixed top-0 left-0 w-72 h-full bg-white shadow-2xl z-50 flex flex-col">
        <div class="flex items-center justify-between px-5 py-4 border-b border-slate-200 bg-slate-50">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 bg-gradient-to-br from-slate-600 to-slate-700 rounded-lg flex items-center justify-center shadow">
              <img src="/favicon.svg" alt="FACET" class="h-5 w-5" />
            </div>
            <span class="text-lg font-semibold text-ink">{{ $t('nav.menuTitle') || 'Menu' }}</span>
          </div>
          <button @click="open = false" aria-label="Close menu" class="p-2 rounded-md hover:bg-slate-200 focus:outline-none focus:ring-2 focus:ring-primary transition">
            <svg class="w-6 h-6 text-ink" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
        
        <!-- User Info Section -->
        <ClientOnly>
          <div v-if="isAuthenticated" class="px-5 py-4 border-b border-slate-100 bg-gradient-to-r from-primary/5 to-primary/10">
            <div class="flex items-center gap-3">
              <div class="w-12 h-12 rounded-full bg-slate-700 text-white flex items-center justify-center font-semibold text-sm shadow-md">
                <span v-if="userInitials">{{ userInitials }}</span>
                <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" viewBox="0 0 20 20" fill="currentColor">
                  <path fill-rule="evenodd" d="M10 2a4 4 0 100 8 4 4 0 000-8zM2 18a8 8 0 1116 0H2z" clip-rule="evenodd" />
                </svg>
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-sm font-semibold text-ink truncate">{{ currentUsername || 'User' }}</div>
                <div class="text-xs text-slate-500">{{ currentRole || 'Member' }}</div>
              </div>
            </div>
          </div>
        </ClientOnly>
        
        <!-- Navigation Content -->
        <div class="flex-1 flex flex-col overflow-y-auto">
          <!-- Quick Actions -->
          <div class="p-4 space-y-2 border-b border-slate-100">
            <div class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">{{ $t('nav.quickActions') || 'Quick Actions' }}</div>
            <NuxtLink to="/" class="nav-link" @click="open = false">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
              {{ $t('nav.home') || 'Home' }}
            </NuxtLink>
            <NuxtLink to="/policies/create" class="nav-link" @click="open = false">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              {{ $t('nav.createPolicy') }}
            </NuxtLink>
            <ClientOnly>
              <NuxtLink v-if="currentRole === 'Admin'" to="/admin" class="nav-link text-orange-700" @click="open = false">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
                {{ $t('nav.manageUsers') }}
              </NuxtLink>
            </ClientOnly>
            <button @click="showLanguageMenu = !showLanguageMenu" class="nav-link flex items-center w-full" :class="{ 'bg-slate-100': showLanguageMenu }">
              <div class="flex items-center gap-2">
                <!-- Modern globe icon -->
                <svg class="w-5 h-5 text-slate-700" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                  <circle cx="12" cy="12" r="9" stroke="currentColor" stroke-width="1.8" />
                  <path d="M3 12h18" stroke="currentColor" stroke-width="1.5" />
                  <path d="M12 3c2.8 3 2.8 15 0 18" stroke="currentColor" stroke-width="1.5" />
                </svg>
                <span>{{ $t('nav.language') || 'Language' }}</span>
                <!-- Chevron positioned to the right of the title -->
                <svg class="w-4 h-4 text-slate-600 transition-transform ml-1" :class="{ 'rotate-180': showLanguageMenu }" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 14l-7 7m0 0l-7-7m7 7V3" />
                </svg>
              </div>
            </button>
            <div v-if="showLanguageMenu" class="space-y-1 border-t border-slate-100 pt-2">
              <button
                v-for="lang in languageList"
                :key="lang.code"
                @click="changeLanguage(lang.code)"
                class="w-full text-left px-3 py-2 rounded-lg text-sm transition-all hover:bg-slate-100"
                :class="{ 'bg-primary/10 text-primary font-medium': currentLanguageCode === lang.code }"
              >
                <span class="font-semibold mr-2">{{ lang.short }}</span>
                <span>{{ lang.name }}</span>
              </button>
            </div>
          </div>

          <!-- Documents Section -->
          <div class="p-4 border-b border-slate-100">
            <div class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3 flex items-center justify-between">
              <span>{{ $t('documents.title') || 'Documents' }}</span>
              <span class="text-primary">{{ uniqueDocs.length }}</span>
            </div>
            <div class="space-y-1 max-h-64 overflow-y-auto">
              <div v-if="uniqueDocs.length === 0" class="text-xs text-slate-400 text-center py-4">
                {{ $t('documents.noDocuments') || 'No documents yet' }}
              </div>
              <button
                v-for="d in uniqueDocs"
                :key="d.id"
                @click="selectDoc(d.id)"
                :class="[
                  'w-full text-left px-3 py-2 rounded-lg text-sm transition-all',
                  selectedId === d.id 
                    ? 'bg-primary/10 text-primary font-medium' 
                    : 'hover:bg-slate-50 text-slate-700'
                ]"
              >
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 min-w-0">
                      <div class="truncate font-medium text-xs">{{ d.fileName }}</div>
                      <span v-if="riskFor(d.id)" :class="['inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[10px] font-semibold uppercase tracking-wide', riskClass(riskFor(d.id)?.level as RiskInfo['level'])]">
                        <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                          <path stroke-linecap="round" stroke-linejoin="round" d="M10.29 3.86l-7.6 13.21A1 1 0 003.42 19h17.16a1 1 0 00.86-1.5L13.84 3.86a1 1 0 00-1.72 0z" />
                          <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v4m0 4h.01" />
                        </svg>
                        <span class="whitespace-nowrap">{{ t(`documents.risk.${riskFor(d.id)?.level}`) }}</span>
                      </span>
                    </div>
                    <div class="text-xs text-slate-400 mt-0.5">{{ (d.sizeBytes/1024).toFixed(1) }} KB</div>
                  </div>
                  <button
                    @click.stop="deleteDoc(d.id)"
                    class="p-1 hover:bg-red-100 rounded text-red-500 transition"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                    <span class="sr-only">{{ $t('documents.delete') }}</span>
                  </button>
                </div>
              </button>
            </div>
          </div>

          <!-- Policies Section -->
          <div class="p-4">
            <div class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3 flex items-center justify-between">
              <span>{{ $t('policies.title') || 'Policies' }}</span>
              <span class="text-primary">{{ policies?.length || 0 }}</span>
            </div>
            <div class="space-y-1 max-h-64 overflow-y-auto">
              <div v-if="!policies || policies.length === 0" class="text-xs text-slate-400 text-center py-4">
                {{ $t('policies.noPolicy') || 'No policies yet' }}
              </div>
              <button
                v-for="p in policies"
                :key="p.id"
                @click="selectPolicy(p.id)"
                :class="[
                  'w-full text-left px-3 py-2 rounded-lg text-sm transition-all',
                  selectedPolicyId === p.id 
                    ? 'bg-emerald-50 text-emerald-700 font-medium' 
                    : 'hover:bg-slate-50 text-slate-700'
                ]"
              >
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <div class="truncate font-medium text-xs">{{ p.name }}</div>
                  </div>
                  <button
                    @click.stop="deletePolicy(p.id)"
                    class="p-1 hover:bg-red-100 rounded text-red-500 transition"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </button>
            </div>
            <button
              v-if="policies && policies.length > 0"
              @click="usePolicyHandler"
              :disabled="!selectedPolicyId"
              class="w-full mt-3 px-3 py-2 bg-emerald-600 hover:bg-emerald-700 active:bg-emerald-800 text-white rounded-lg text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed transition"
            >
              {{ $t('policies.usePolicy') }}
            </button>
          </div>
        </div>
        
        <!-- Logout Button at Bottom -->
        <ClientOnly>
          <div v-if="isAuthenticated" class="p-4 border-t border-slate-200">
            <button 
              @click="logout" 
              class="w-full flex items-center gap-2 px-3 py-2 rounded-lg border border-slate-200 text-ink hover:bg-slate-50 active:bg-slate-100 transition-colors"
            >
              <svg class="w-5 h-5 text-slate-700" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              <span class="font-medium">{{ $t('nav.logout') }}</span>
            </button>
          </div>
        </ClientOnly>
      </aside>
    </transition>
  </nav>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuth } from '~/composables/useAuth'
import LanguageSwitcher from './LanguageSwitcher.vue'
import type { Document, RiskInfo } from '~/types'

const props = defineProps<{
  docs?: Document[]
  policies?: { id: string, name: string, options: Record<string, boolean> }[]
  selectedId?: string | null
  selectedPolicyId?: string | null
  riskByDoc?: Record<string, RiskInfo>
}>()

const emit = defineEmits<{
  (e: 'selectDoc', id: string): void
  (e: 'deleteDoc', id: string): void
  (e: 'selectPolicy', id: string): void
  (e: 'deletePolicy', id: string): void
  (e: 'usePolicy'): void
}>()

const open = ref(false)
const showLanguageMenu = ref(false)
const router = useRouter()
const { logOut, isAuthenticated, currentUsername, currentRole } = useAuth()
const { locale, t } = useI18n()

const languageList = [
  { code: 'en', short: 'Eng', name: 'English' },
  { code: 'ru', short: 'Рус', name: 'Русский' },
  { code: 'est', short: 'Est', name: 'Eesti' }
]

const currentLanguageCode = computed(() => locale.value)

// Deduplicate documents by id to avoid duplicates in the mobile menu
const uniqueDocs = computed<Document[]>(() => {
  const seen = new Set<string>()
  const out: Document[] = []
  for (const d of (props.docs || [])) {
    if (!d?.id || seen.has(d.id)) continue
    seen.add(d.id)
    out.push(d)
  }
  return out
})

const userInitials = computed(() => {
  const name = currentUsername.value || ''
  if (!name) return ''
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const second = parts[1]?.[0] ?? ''
  return (first + second).toUpperCase()
})

function logout() {
  open.value = false
  logOut()
}

function changeLanguage(code: string) {
  locale.value = code
  if (process.client) {
    localStorage.setItem('language', code)
    document.documentElement.lang = code
  }
  showLanguageMenu.value = false
}

function selectDoc(id: string) {
  emit('selectDoc', id)
  open.value = false
}

function deleteDoc(id: string) {
  emit('deleteDoc', id)
}

function selectPolicy(id: string) {
  emit('selectPolicy', id)
}

function deletePolicy(id: string) {
  emit('deletePolicy', id)
}

function usePolicyHandler() {
  emit('usePolicy')
  open.value = false
}

function riskFor(id: string) {
  return props.riskByDoc?.[id]
}

function riskClass(level: RiskInfo['level']) {
  if (level === 'high') return 'bg-red-100 text-red-700 border border-red-200'
  if (level === 'medium') return 'bg-orange-100 text-orange-700 border border-orange-200'
  return 'bg-emerald-100 text-emerald-700 border border-emerald-200'
}

function dotClass(level: RiskInfo['level']) {
  if (level === 'high') return 'bg-red-500'
  if (level === 'medium') return 'bg-orange-500'
  return 'bg-emerald-500'
}
</script>

<style scoped>
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.2s;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
.slide-enter-active, .slide-leave-active {
  transition: transform 0.25s cubic-bezier(0.4,0,0.2,1);
}
.slide-enter-from, .slide-leave-to {
  transform: translateX(-100%);
}
.nav-link {
  display: block;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  font-size: 1rem;
  color: #0b1220;
  text-decoration: none;
  transition: background 0.15s, color 0.15s;
}
.nav-link:hover, .nav-link:focus {
  background: #f1f5f9;
  color: #0ea5e9;
}
</style>
