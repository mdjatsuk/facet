<script setup lang="ts">
import { computed, ref, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useDetected } from '../composables/useDetected'
import { useSelection } from '../composables/useSelection'
import type { Document, SensitiveItem } from '../types'

// Enable pinch-to-zoom on mobile
if (process.client) {
  const viewport = document.querySelector('meta[name="viewport"]')
  if (viewport) {
    viewport.setAttribute('content', 'width=device-width, initial-scale=1, maximum-scale=5, user-scalable=yes')
  }
}

const route = useRoute()
const router = useRouter()
const { get, post, del } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const docId = computed(() => route.query.docId as string || '')
const isStaged = computed(() => route.query.staged === 'true')

const doc = ref<Document | null>(null)
const { getDetected, clearDetected } = useDetected()
const { getValues, clear } = useSelection()

const previewUrl = computed(() => 
  doc.value ? `${apiBase}/documents/${doc.value.id}/file` : ''
)
const previewType = computed(() => doc.value?.contentType || '')

const currentDetected = computed(() => getDetected(docId.value) || [])
const uniqueTypes = computed(() => [...new Set(currentDetected.value.map(d => d.type))])
const selectedCount = computed(() => (getValues(docId.value) || []).length)

async function loadDocument() {
  if (!docId.value) {
    router.push('/')
    return
  }
  try {
    doc.value = await get<Document>(`documents/${docId.value}`)
  } catch (e) {
    console.error('Failed to load document', e)
    router.push('/')
  }
}

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
    
    // Clear document data and selections
    clearDetected(docId.value)
    clear(docId.value)
    
    // If this was a staged document, clear it from localStorage
    if (isStaged.value) {
      const { currentUsername } = useAuth()
      const stagedDocKey = `stagedDoc_${currentUsername.value ?? 'anon'}`
      localStorage.removeItem(stagedDocKey)
    }
    
    // Get the new document ID and details
    const newDoc = res?.document || res?.Document
    const newDocId = newDoc?.id
    const detectedItems = res?.detected || res?.Detected || []
    
    if (!newDocId) {
      throw new Error('No document ID returned from server')
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
    
    // Store the new doc and detected items in sessionStorage to pass to home page
    sessionStorage.setItem('newRedactedDoc', JSON.stringify({ newDoc, detectedItems }))
    
    // Navigate back to home page with the new document selected
    router.push({ path: '/', query: { docId: newDocId } })
  } catch (e: any) {
    console.error('Save redact failed', e)
    const errorMsg = e?.data?.message || e?.message || 'Failed to create redacted copy. Please check if you have selected items to redact.'
    alert(errorMsg)
  }
}

async function discardStaged() {
  if (!docId.value || !isStaged.value) return
  
  try {
    await del(`documents/${docId.value}`)
    clearDetected(docId.value)
    clear(docId.value)
    router.push('/')
  } catch (e) {
    console.error('Failed to discard document', e)
    alert('Failed to discard document')
  }
}

function goBack() {
  // Pass the current docId back to the index page so it can select the document
  if (docId.value) {
    router.push({ path: '/', query: { docId: docId.value } })
  } else {
    router.push('/')
  }
}

// Watch for docId changes to reload the document
watch(docId, (newDocId) => {
  if (newDocId) {
    loadDocument()
  }
})

onMounted(() => {
  loadDocument()
})
</script>

<template>
  <ClientOnly>
    <div class="min-h-screen bg-slate-50">
      <!-- Header -->
      <header class="bg-white border-b border-slate-100 shadow-sm sticky top-0 z-50">
        <div class="max-w-7xl mx-auto px-4 py-3 flex items-center justify-between gap-3">
          <button @click="goBack" class="p-2 hover:bg-slate-100 rounded-lg transition">
            <svg class="w-6 h-6 text-slate-700" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <div class="flex-1 min-w-0 text-center">
            <h1 class="text-lg font-semibold text-slate-800 truncate">{{ doc?.fileName || 'Preview' }}</h1>
          </div>
          <div class="w-10"></div>
        </div>
      </header>

      <!-- Document Info Banner (for staged docs) -->
      <div v-if="isStaged && doc" class="bg-amber-50 border-b border-amber-100">
        <div class="max-w-7xl mx-auto px-4 py-3">
          <div class="flex items-center justify-between gap-3">
            <div class="flex-1 min-w-0">
              <div class="text-sm font-medium text-amber-900">Staged Document</div>
              <div class="text-xs text-amber-700 mt-0.5">
                {{ new Date(doc.uploadedAt).toLocaleString() }} • {{ (doc.sizeBytes / 1024).toFixed(2) }} KB
              </div>
            </div>
            <button 
              @click="discardStaged" 
              class="px-3 py-1.5 bg-red-500 text-white text-xs rounded-md hover:bg-red-600 active:bg-red-700 transition touch-manipulation whitespace-nowrap"
            >
              Remove
            </button>
          </div>
        </div>
      </div>

      <!-- Main Content -->
      <main class="max-w-7xl mx-auto">
        <!-- Full Screen Preview Container -->
        <div class="relative">
          <PreviewPane 
            v-if="doc" 
            :key="doc.id"
            :url="previewUrl" 
            :contentType="previewType" 
            :documentId="doc.id"
            :fullScreen="true"
          />
          <div v-else class="fixed inset-0 top-14 flex items-center justify-center bg-slate-50">
            <div class="text-center">
              <svg class="w-12 h-12 mx-auto text-slate-400 mb-2 animate-pulse" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
              </svg>
              <div class="text-slate-500">Loading preview...</div>
            </div>
          </div>
        </div>

        <!-- Floating Action Buttons -->
        <div class="fixed bottom-0 left-0 right-0 bg-white border-t border-slate-200 shadow-lg p-4 z-50">
          <div class="max-w-7xl mx-auto">
            <div class="grid grid-cols-2 gap-3">
              <button 
                @click="goBack"
                class="px-4 py-3 bg-slate-100 text-slate-700 rounded-lg hover:bg-slate-200 active:bg-slate-300 touch-manipulation text-sm font-medium transition"
              >
                Back
              </button>
              <NuxtLink
                :to="{ path: '/sensitive-data', query: { docId: docId, staged: isStaged ? 'true' : undefined } }"
                class="px-4 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 active:bg-blue-800 touch-manipulation text-sm font-medium transition text-center flex items-center justify-center gap-2"
              >
                <svg class="w-5 h-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 11c0-1.38-1.12-2.5-2.5-2.5S7 9.62 7 11s1.12 2.5 2.5 2.5S12 12.38 12 11z" />
                </svg>
                Sensitive Data
                <span v-if="uniqueTypes.length > 0" class="bg-white text-blue-600 rounded-full px-2 py-0.5 text-xs font-bold">{{ uniqueTypes.length }}</span>
              </NuxtLink>
            </div>
          </div>
        </div>
      </main>
    </div>
  </ClientOnly>
</template>
