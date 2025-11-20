<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDetected } from '../composables/useDetected'
import { useSelection } from '../composables/useSelection'

const route = useRoute()
const router = useRouter()
const { post } = useApi()

const docId = computed(() => route.query.docId as string || '')
const isStaged = computed(() => route.query.staged === 'true')

const { getDetected, clearDetected } = useDetected()
const { getValues, clear } = useSelection()

const currentDetected = computed(() => getDetected(docId.value) || [])
const uniqueTypes = computed(() => [...new Set(currentDetected.value.map(d => d.type))])
const selectedCount = computed(() => (getValues(docId.value) || []).length)

async function applyChanges() {
  if (!docId.value) return
  const vals = getValues(docId.value)
  if (!vals || vals.length === 0) {
    alert('No selections for this document')
    return
  }
  
  try {
    const res = await post<any>(`documents/${docId.value}/redact/save`, { values: vals })
    console.log('Redacted copy created', res)
    
    // Get the new redacted document ID from response
    const newDocId = res?.document?.id || res?.Document?.id
    
    // Clear old document data and selections
    clearDetected(docId.value)
    clear(docId.value)
    
    // If this was a staged document, clear it from localStorage
    if (isStaged.value) {
      const { currentUsername } = useAuth()
      const stagedDocKey = `stagedDoc_${currentUsername.value ?? 'anon'}`
      localStorage.removeItem(stagedDocKey)
    }
    
    // For anonymous users, save the new document ID to localStorage
    const { isAuthenticated } = useAuth()
    if (!isAuthenticated.value && newDocId) {
      const anonDocsKey = 'anonDocs'
      const storedIds = JSON.parse(localStorage.getItem(anonDocsKey) || '[]') as string[]
      if (!storedIds.includes(newDocId)) {
        storedIds.push(newDocId)
        localStorage.setItem(anonDocsKey, JSON.stringify(storedIds))
      }
      // Remove old document from anonymous docs
      const filtered = storedIds.filter(id => id !== docId.value)
      localStorage.setItem(anonDocsKey, JSON.stringify(filtered))
    }
    
    // Navigate to preview page with the new document
    if (newDocId) {
      router.push({ path: '/preview', query: { docId: newDocId } })
    } else {
      router.push('/preview')
    }
  } catch (e: any) {
    console.error('Save redact failed', e)
    const errorMsg = e?.data?.message || e?.message || 'Failed to create redacted copy. Please check if you have selected items to redact.'
    alert(errorMsg)
  }
}

function goBack() {
  router.push({ path: '/preview', query: { docId: docId.value, staged: isStaged.value ? 'true' : undefined } })
}

onMounted(() => {
  if (!docId.value) {
    router.push('/')
  }
})
</script>

<template>
  <ClientOnly>
    <div class="min-h-screen bg-slate-50 pb-24">
      <!-- Header -->
      <header class="bg-white border-b border-slate-100 shadow-sm sticky top-0 z-50">
        <div class="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between gap-3">
          <button @click="goBack" class="p-2 hover:bg-slate-100 rounded-lg transition">
            <svg class="w-6 h-6 text-slate-700" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <div class="flex-1 min-w-0 text-center">
            <h1 class="text-lg font-semibold text-slate-800">Sensitive Data</h1>
          </div>
          <div class="w-10"></div>
        </div>
      </header>

      <!-- Main Content -->
      <main class="max-w-7xl mx-auto px-4 py-6">
        <!-- Selection Summary -->
        <div class="bg-white rounded-lg shadow-sm border border-slate-200 p-4 mb-6">
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-12 h-12 bg-blue-100 rounded-full flex items-center justify-center">
                <svg class="w-6 h-6 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
              </div>
              <div>
                <div class="text-sm font-semibold text-slate-900">Items Selected</div>
                <div class="text-xs text-slate-500">Ready for redaction</div>
              </div>
            </div>
            <div class="text-3xl font-bold text-blue-600">{{ selectedCount }}</div>
          </div>
        </div>

        <!-- Sensitive Data Types -->
        <div class="space-y-3">
          <h2 class="text-sm font-semibold text-slate-700 px-1">Detected Sensitive Data Types</h2>
          
          <div v-if="uniqueTypes.length === 0" class="bg-white rounded-lg shadow-sm border border-slate-200 p-8 text-center">
            <svg class="w-16 h-16 mx-auto text-slate-300 mb-3" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
            <div class="text-slate-500 text-sm">No sensitive data detected in this document</div>
          </div>

          <div v-else class="space-y-3">
            <NuxtLink
              v-for="t in uniqueTypes"
              :key="t"
              :to="{ path: `/sensitive/${encodeURIComponent(t)}`, query: { docId: docId, from: 'sensitive-data' } }"
              class="block bg-white rounded-lg shadow-sm border border-slate-200 p-4 hover:shadow-md hover:border-blue-300 active:scale-98 transition touch-manipulation"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-3 flex-1 min-w-0">
                  <div class="w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center flex-shrink-0">
                    <svg class="w-5 h-5 text-blue-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 11c0-1.38-1.12-2.5-2.5-2.5S7 9.62 7 11s1.12 2.5 2.5 2.5S12 12.38 12 11z" />
                    </svg>
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="text-sm font-semibold text-slate-900 capitalize truncate">{{ t }}</div>
                    <div class="text-xs text-slate-500">
                      {{ currentDetected.filter(d => d.type === t).length }} item(s) detected
                    </div>
                  </div>
                </div>
                <svg class="w-5 h-5 text-slate-400 flex-shrink-0" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                </svg>
              </div>
            </NuxtLink>
          </div>
        </div>

        <!-- Instructions -->
        <div v-if="uniqueTypes.length > 0" class="mt-6 bg-blue-50 border border-blue-200 rounded-lg p-4">
          <div class="flex gap-3">
            <svg class="w-5 h-5 text-blue-600 flex-shrink-0 mt-0.5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <div class="text-sm text-blue-900">
              <p class="font-semibold mb-1">How to redact sensitive data:</p>
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>Tap on a sensitive data type above</li>
                <li>Select items you want to redact</li>
                <li>Return here and click "Apply Changes"</li>
              </ol>
            </div>
          </div>
        </div>
      </main>

      <!-- Floating Action Buttons -->
      <div class="fixed bottom-0 left-0 right-0 bg-white border-t border-slate-200 shadow-lg p-4 z-50">
        <div class="max-w-7xl mx-auto">
          <div class="grid grid-cols-2 gap-3">
            <button 
              @click="goBack"
              class="px-4 py-3 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 active:bg-slate-300 touch-manipulation text-sm font-medium transition"
            >
              Back to Preview
            </button>
            <button 
              @click="applyChanges" 
              :disabled="selectedCount === 0"
              class="px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 disabled:cursor-not-allowed touch-manipulation text-sm font-medium transition"
            >
              Apply Changes
            </button>
          </div>
        </div>
      </div>
    </div>
  </ClientOnly>
</template>
