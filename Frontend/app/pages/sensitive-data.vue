<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useDetected } from '../composables/useDetected'
import { useSelection } from '../composables/useSelection'
import type { SensitiveItem } from '../types'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()
const { post } = useApi()

const docId = computed(() => route.query.docId as string || '')
const isStaged = computed(() => route.query.staged === 'true')

const { getDetected, setDetected, clearDetected } = useDetected()
const { getValues, clear } = useSelection()

const currentDetected = computed(() => getDetected(docId.value) || [])
const uniqueTypes = computed(() => [...new Set(currentDetected.value.map(d => d.type))])
const selectedCount = computed(() => (getValues(docId.value) || []).length)

// No custom scan logic here; reuse shared component

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
    
    // Get the new redacted document ID and detected items from response
    const newDoc = res?.document || res?.Document
    const newDocId = newDoc?.id
    const detectedItems = res?.detected || res?.Detected || []
    
    if (!newDocId) {
      throw new Error('No document ID returned from server')
    }
    
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
      // Remove old document from anonymous docs only if id changed
      if (newDocId !== docId.value) {
        const filtered = storedIds.filter(id => id !== docId.value)
        localStorage.setItem(anonDocsKey, JSON.stringify(filtered))
      }
    }
    
    // Store the new doc and detected items in sessionStorage to pass to home page
    // Persist info for index page to update list without duplication
    const payload = { newDoc, detectedItems, oldDocId: docId.value }
    sessionStorage.setItem('newRedactedDoc', JSON.stringify(payload))
    // Also dispatch a realtime event so home page can update without reload
    window.dispatchEvent(new CustomEvent('facet:new-redacted-doc', { detail: payload }))
    
    // ALSO set detected items directly in the state so they're available immediately
    if (detectedItems && detectedItems.length > 0) {
      setDetected(newDocId, detectedItems)
    }
    
    // Navigate back to main page with the new document selected (for mobile)
    router.push({ path: '/', query: { docId: newDocId } })
  } catch (e: any) {
    console.error('Save redact failed', e)
    const errorMsg = e?.data?.message || e?.message || 'Failed to create redacted copy. Please check if you have selected items to redact.'
    alert(errorMsg)
  }
}

function goBack() {
  // Store detected items before navigating away (in case user made changes with custom pattern search)
  // This preserves the detected data when going back to preview
  if (docId.value && currentDetected.value && currentDetected.value.length > 0) {
    sessionStorage.setItem('detectedItemsOnReturn', JSON.stringify({
      docId: docId.value,
      items: currentDetected.value,
      timestamp: Date.now()
    }))
  } else {
    sessionStorage.removeItem('detectedItemsOnReturn')
  }
  
  // Navigate back to preview page with the current docId and staged status
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
            <h1 class="text-lg font-semibold text-slate-800">{{ $t('sensitiveData.title') }}</h1>
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
                <div class="text-sm font-semibold text-slate-900">{{ $t('sensitiveData.selected') }}</div>
                <div class="text-xs text-slate-500">{{ $t('sensitiveData.readyForRedaction') || 'Ready for redaction' }}</div>
              </div>
            </div>
            <div class="text-3xl font-bold text-blue-600">{{ selectedCount }}</div>
          </div>
        </div>

        <!-- Custom Pattern Search (Mobile Optimized) -->
        <div class="bg-white rounded-lg shadow-sm border border-slate-200 p-4 mb-6">
          <CustomPatternSearch :doc-id="docId" @patternFound="() => {}" />
        </div>

        <!-- Sensitive Data Types -->
        <div class="space-y-3">
          <h2 class="text-sm font-semibold text-slate-700 px-1">{{ $t('sensitiveData.detectedSensitiveTypes') || 'Detected Sensitive Data Types' }}</h2>
          
          <div v-if="uniqueTypes.length === 0" class="bg-white rounded-lg shadow-sm border border-slate-200 p-8 text-center">
            <svg class="w-16 h-16 mx-auto text-slate-300 mb-3" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
            </svg>
            <div class="text-slate-500 text-sm">{{ $t('sensitiveData.noDataDoc') || 'No sensitive data detected in this document' }}</div>
          </div>

          <div v-else class="space-y-3">
            <NuxtLink
              v-for="t in uniqueTypes"
              :key="t"
              :to="{ path: `/sensitive/${encodeURIComponent(t)}`, query: { docId: docId, staged: isStaged ? 'true' : undefined, from: 'sensitive-data' } }"
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
                      {{ $t('sensitiveData.itemsDetected', { count: currentDetected.filter(d => d.type === t).length }) || (currentDetected.filter(d => d.type === t).length + ' item(s) detected') }}
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
              <p class="font-semibold mb-1">{{ $t('sensitiveData.instructions.title') || 'How to redact sensitive data:' }}</p>
              <ol class="list-decimal list-inside space-y-1 text-xs">
                <li>{{ $t('sensitiveData.instructions.step1') || 'Tap on a sensitive data type above' }}</li>
                <li>{{ $t('sensitiveData.instructions.step2') || 'Select items you want to redact' }}</li>
                <li>{{ $t('sensitiveData.instructions.step3') || 'Return here and click "Apply Changes"' }}</li>
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
              {{ $t('preview.backToPreview') || 'Back to Preview' }}
            </button>
            <button 
              @click="applyChanges" 
              :disabled="selectedCount === 0"
              class="px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 active:bg-blue-800 disabled:opacity-50 disabled:cursor-not-allowed touch-manipulation text-sm font-medium transition"
            >
              {{ $t('sensitiveData.apply') }}
            </button>
          </div>
        </div>
      </div>
    </div>
  </ClientOnly>
</template>
