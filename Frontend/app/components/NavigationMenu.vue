<template>
  <nav>
    <!-- Mobile Nav: Hamburger -->
    <div class="lg:hidden flex items-center justify-between px-4 py-3 bg-white border-b border-slate-100 shadow-sm sticky top-0 z-50">
      <button @click="open = true" aria-label="Open menu" class="p-2 rounded-md hover:bg-slate-50 focus:outline-none focus:ring-2 focus:ring-primary transition">
        <svg class="w-7 h-7 text-ink" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 bg-gradient-to-br from-slate-600 to-slate-700 rounded-lg flex items-center justify-center shadow">
          <img src="/favicon.svg" alt="FACET" class="h-5 w-5" />
        </div>
        <span class="text-xl font-semibold text-ink">FACET</span>
      </div>
      <div class="w-11"></div> <!-- Spacer for centering -->
    </div>

    <!-- Overlay -->
    <transition name="fade">
      <div v-if="open" class="fixed inset-0 bg-black bg-opacity-50 z-40" @click="open = false"></div>
    </transition>

    <!-- Drawer -->
    <transition name="slide">
      <aside v-if="open" class="fixed top-0 left-0 w-72 h-full bg-white shadow-2xl z-50 flex flex-col">
        <div class="flex items-center justify-between px-5 py-4 border-b border-slate-200 bg-slate-50">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 bg-gradient-to-br from-slate-600 to-slate-700 rounded-lg flex items-center justify-center shadow">
              <img src="/favicon.svg" alt="FACET" class="h-5 w-5" />
            </div>
            <span class="text-lg font-semibold text-ink">Menu</span>
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
            <div class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-2">Quick Actions</div>
            <NuxtLink to="/" class="nav-link" @click="open = false">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
              Home
            </NuxtLink>
            <NuxtLink to="/policies/create" class="nav-link" @click="open = false">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              Create Policy
            </NuxtLink>
            <ClientOnly>
              <NuxtLink v-if="currentRole === 'Admin'" to="/admin" class="nav-link text-orange-700" @click="open = false">
                <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
                </svg>
                Manage Users
              </NuxtLink>
            </ClientOnly>
          </div>

          <!-- Documents Section -->
          <div class="p-4 border-b border-slate-100">
            <div class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3 flex items-center justify-between">
              <span>Documents</span>
              <span class="text-primary">{{ docs?.length || 0 }}</span>
            </div>
            <div class="space-y-1 max-h-64 overflow-y-auto">
              <div v-if="!docs || docs.length === 0" class="text-xs text-slate-400 text-center py-4">
                No documents yet
              </div>
              <button
                v-for="d in docs"
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
                    <div class="truncate font-medium text-xs">{{ d.fileName }}</div>
                    <div class="text-xs text-slate-400 mt-0.5">{{ (d.sizeBytes/1024).toFixed(1) }} KB</div>
                  </div>
                  <button
                    @click.stop="deleteDoc(d.id)"
                    class="p-1 hover:bg-red-100 rounded text-red-500 transition"
                  >
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                    </svg>
                  </button>
                </div>
              </button>
            </div>
          </div>

          <!-- Policies Section -->
          <div class="p-4">
            <div class="text-xs font-semibold text-slate-500 uppercase tracking-wider mb-3 flex items-center justify-between">
              <span>Policies</span>
              <span class="text-primary">{{ policies?.length || 0 }}</span>
            </div>
            <div class="space-y-1 max-h-64 overflow-y-auto">
              <div v-if="!policies || policies.length === 0" class="text-xs text-slate-400 text-center py-4">
                No policies yet
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
              Use Selected Policy
            </button>
          </div>
        </div>
        
        <!-- Logout Button at Bottom -->
        <ClientOnly>
          <div v-if="isAuthenticated" class="p-4 border-t border-slate-200">
            <button @click="logout" class="nav-link text-red-600 w-full justify-start">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
              Logout
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
import { useAuth } from '~/composables/useAuth'
import type { Document } from '~/types'

const props = defineProps<{
  docs?: Document[]
  policies?: { id: string, name: string, options: Record<string, boolean> }[]
  selectedId?: string | null
  selectedPolicyId?: string | null
}>()

const emit = defineEmits<{
  (e: 'selectDoc', id: string): void
  (e: 'deleteDoc', id: string): void
  (e: 'selectPolicy', id: string): void
  (e: 'deletePolicy', id: string): void
  (e: 'usePolicy'): void
}>()

const open = ref(false)
const router = useRouter()
const { logOut, isAuthenticated, currentUsername, currentRole } = useAuth()

const userInitials = computed(() => {
  const name = currentUsername.value || ''
  if (!name) return ''
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const second = parts[1]?.[0] ?? ''
  return (first + second).toUpperCase()
})

function logout() {
  logOut()
  open.value = false
  router.push('/')
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
