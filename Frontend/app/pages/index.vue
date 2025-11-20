<script setup lang="ts">
import { onMounted, watch, computed } from 'vue'
import { useDetected } from '../composables/useDetected'
import { useSelection } from '../composables/useSelection'
import type { Document, SensitiveItem } from '../types'

const { get, del, post } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const docs = ref<Document[]>([])
const selectedId = ref<string | null>(null)
// Staged upload: shown in preview until Apply Changes creates redacted copy
const stagedDoc = ref<Document | null>(null)

const { setDetected, getDetected, clearDetected } = useDetected()

const policies = ref<{ id: string, name: string, options: Record<string, boolean> }[]>([])
const selectedPolicyId = ref<string | null>(null)

const selectedDoc = computed(() => 
  docs.value.find(d => d.id === selectedId.value) || docs.value[0] || null
)
const previewUrl = computed(() => {
  const doc = stagedDoc.value || selectedDoc.value
  return doc ? `${apiBase}/documents/${doc.id}/file` : ''
})
const previewType = computed(() => {
  const doc = stagedDoc.value || selectedDoc.value
  return doc?.contentType || ''
})

const currentDetected = computed(() => {
  const docId = stagedDoc.value?.id || selectedId.value || ''
  return getDetected(docId) || []
})
const uniqueTypes = computed(() => [...new Set(currentDetected.value.map(d => d.type))])

const route = useRoute()
const notice = ref<string | null>(null)

const { getValues, clear, setAll } = useSelection()
const selectedCount = computed(() => {
  const docId = stagedDoc.value?.id || selectedId.value || ''
  return (getValues(docId) || []).length
})

const viewedPolicy = ref<{ id: string, name: string, options: Record<string, boolean> } | null>(null)

const optionLabels: Record<string, string> = {
  deleteAllEmails: 'Emails',
  removePhoneNumbers: 'Phone Number',
  removeNationalIds: 'ID',
  anonymizeNames: 'Names',
  removeMailingAddresses: 'Address',
  deleteIPAddresses: 'IP Address',
  removeFinancialInfo: 'Financial Info',
  stripMedicalInfo: 'Medical Info',
  removeUsernames: 'Usernames',
}

const activeOptions = computed(() => {
  if (!viewedPolicy.value) return [] as (keyof typeof optionLabels)[]
  return (Object.keys(viewedPolicy.value.options) as (keyof typeof optionLabels)[])
    .filter(k => viewedPolicy.value?.options[k])
})

// Notice from route query 'policy-created' removed to avoid auto-showing a success message after creating a policy.

async function refresh(selectNewId?: string) {
  // For authenticated users, fetch from API
  if (isAuthenticated.value) {
    docs.value = await get<Document[]>('documents')
  } else {
    // For anonymous users, load documents from localStorage
    const anonDocsKey = 'anonDocs'
    const storedIds = JSON.parse(localStorage.getItem(anonDocsKey) || '[]') as string[]
    const loadedDocs = []
    for (const id of storedIds) {
      try {
        const doc = await get<Document>(`documents/${id}`)
        loadedDocs.push(doc)
      } catch (e) {
        console.error(`Failed to load anonymous document ${id}`, e)
      }
    }
    docs.value = loadedDocs
  }
  
  if (selectNewId) {
    selectedId.value = selectNewId
  } else if (!selectedId.value && docs.value.length > 0) {
    selectedId.value = docs.value[0]?.id || null
  } else if (selectedId.value && !docs.value.some(d => d.id === selectedId.value) && docs.value.length > 0) {
    selectedId.value = docs.value[0]?.id || null
  }
}

function viewPolicy(id: string) {
  viewedPolicy.value = policies.value.find(p => p.id === id) || null
}

async function onDelete(id: string) {
  await del(`documents/${id}`)
  clearDetected(id)
  // Remove from anonymous docs if not authenticated
  if (!isAuthenticated.value) {
    removeAnonDoc(id)
  }
  await refresh() 
}

function onUploaded(payload: { document: Document, detected?: SensitiveItem[] }) {
  console.log('Upload response payload:', payload)
  console.log('Detected items:', payload.detected)
  // Stage the uploaded document (shown in preview but not in list)
  stagedDoc.value = payload.document
  // Clear any previous selections for the new document to ensure preview shows original content
  clear(payload.document.id)
  setDetected(payload.document.id, payload.detected || [])  
  console.log('Detected for doc:', payload.document.id, payload.detected)
}

watch(selectedId, () => {
  console.log('Selected doc changed to:', selectedId.value)
})

// Scope stagedDoc and policies in localStorage per-user so different accounts don't see each other's data
const { currentUsername, isAuthenticated, logOut, currentRole } = useAuth()
const userInitials = computed(() => {
  const name = currentUsername.value || ''
  if (!name) return ''
  // Take first letter(s) of first two words
  const parts = name.trim().split(/\s+/)
  const first = parts[0]?.[0] ?? ''
  const second = parts[1]?.[0] ?? ''
  return (first + second).toUpperCase()
})
const stagedDocKey = computed(() => `stagedDoc_${currentUsername.value ?? 'anon'}`)
const policiesKey = computed(() => `policies_${currentUsername.value ?? 'anon'}`)

// Persist stagedDoc to localStorage
watch(stagedDoc, (newVal) => {
  if (newVal) {
    localStorage.setItem(stagedDocKey.value, JSON.stringify(newVal))
  } else {
    localStorage.removeItem(stagedDocKey.value)
  }
}, { deep: true })

// Restore stagedDoc from localStorage on mount
onMounted(() => {
  const saved = localStorage.getItem(stagedDocKey.value)
  if (saved) {
    try {
      stagedDoc.value = JSON.parse(saved)
      console.log('Restored staged doc from localStorage:', stagedDoc.value)
    } catch (e) {
      console.error('Failed to restore staged doc', e)
      localStorage.removeItem(stagedDocKey.value)
    }
  }
})

function deletePolicy(id: string) {
  policies.value = policies.value.filter(p => p.id !== id)
  localStorage.setItem(policiesKey.value, JSON.stringify(policies.value))
}

function selectPolicy(id: string) {
  selectedPolicyId.value = id
}

onMounted(() => 
{
  try {
    policies.value = JSON.parse(localStorage.getItem(policiesKey.value) || '[]')
  } catch (e) {
    policies.value = []
  }
  const initialDocId = (route.query.docId as string) || undefined
  refresh(initialDocId)
})

// Watch for route query changes (e.g., when returning from preview with new docId)
watch(() => route.query.docId, (newDocId) => {
  if (newDocId) {
    refresh(newDocId as string)
  }
})

// Reload policies when currentUsername changes (login/logout/switch accounts)
watch(currentUsername, () => {
  try {
    policies.value = JSON.parse(localStorage.getItem(policiesKey.value) || '[]')
  } catch (e) {
    policies.value = []
  }
})

async function usePolicy(id?: string) {
  const pid = id ?? selectedPolicyId.value
  if (!pid) {
    notice.value = 'No policy selected'
    return
  }
  const policy = policies.value.find(p => p.id === pid)
  if (!policy) {
    notice.value = 'Policy not found'
    return
  }
  const targetDocId = stagedDoc.value?.id ?? selectedId.value
  if (!targetDocId) {
    notice.value = 'No document selected'
    return
  }

  const typeToOptionKey: Record<string, string> = {
    email: 'deleteAllEmails',
    phone: 'removePhoneNumbers',
    id: 'removeNationalIds',
    iban: 'removeFinancialInfo',
  }

  const values = currentDetected.value
    .filter(d => {
      const opt = typeToOptionKey[d.type]
      return opt ? !!policy.options[opt] : false
    })
    .map(d => d.value)
    .filter(Boolean)

  if (values.length === 0) {
    notice.value = `Policy "${policy.name}" did not match any items in the document`
    return
  }

  clear(targetDocId)
  setAll(targetDocId, values)

  try {
    await applySelected()
  }
  catch (e) {
    console.error('UsePolicy applySelected failed', e)
    alert(`Failed to apply policy "${policy.name}"`)
  }
}

async function applySelected() {
  const targetId = stagedDoc.value?.id ?? selectedId.value
  if (!targetId) return
  const vals = getValues(targetId)
  console.log('ApplySelected called for', targetId, 'values:', vals)
  if (!vals || vals.length === 0) { alert('No selections for the selected document'); return }
  try {
    const res = await post<any>(`documents/${targetId}/redact/save`, { values: vals })
    console.log('Redacted copy created', res)
    const newDoc = res?.document || res?.Document
    const newDocId = newDoc?.id
    const detectedItems = res?.detected || res?.Detected || []
    
    if (newDocId && newDoc) {
      // Clear old document data and selections
      clearDetected(targetId)
      clear(targetId)
      
      // Set detected items for new redacted document
      if (detectedItems.length > 0) {
        setDetected(newDocId, detectedItems)
      }
      
      // If this was a staged document, clear it immediately (this will also clear localStorage via watcher)
      if (stagedDoc.value?.id === targetId) {
        stagedDoc.value = null
      }
      
      // For anonymous users, save the new document ID to localStorage
      if (!isAuthenticated.value && newDocId) {
        saveAnonDoc(newDocId)
        // Remove the old staged document from anonymous docs
        removeAnonDoc(targetId)
      }
      
      // Refresh the documents list from server to get accurate state
      await refresh(newDocId)
      
      console.log('Applied changes: new doc', newDocId, 'is now selected')
    } else {
      await refresh()
    }
  }
  catch (e: any) {
    console.error('Save redact failed', e)
    const errorMsg = e?.data?.message || e?.message || 'Failed to create redacted copy. Please check if you have selected items to redact.'
    alert(errorMsg)
    return
  }
}

async function discardStaged() {
  if (!stagedDoc.value?.id) return
  const docId = stagedDoc.value.id
  try {
    await del(`documents/${docId}`)
    console.log('Staged document discarded')
    stagedDoc.value = null
    clearDetected(docId)
    clear(docId)
  }
  catch (e: any) {
    console.error('Failed to discard staged document', e)
    // If 404, document was already deleted (e.g., after redaction), just clear the state
    if (e?.response?.status === 404) {
      console.log('Document already deleted, clearing staged state')
      stagedDoc.value = null
      clearDetected(docId)
      clear(docId)
    } else {
      const errorMsg = e?.response?.data?.message || e?.message || 'Failed to discard staged document'
      alert(errorMsg)
    }
  }
}

</script>

<template>
  <!-- Render only on client to avoid server rendering protected page before auth is ready -->
  <ClientOnly>
    <div class="min-h-screen">
      <!-- Mobile Navigation Menu (hamburger) -->
      <NavigationMenu 
        class="lg:hidden" 
        :docs="docs" 
        :policies="policies"
        :selected-id="selectedId"
        :selected-policy-id="selectedPolicyId"
        @select-doc="(id) => { selectedId = id }"
        @delete-doc="onDelete"
        @select-policy="selectPolicy"
        @delete-policy="deletePolicy"
        @use-policy="() => usePolicy()"
      />
      
      <!-- Desktop Header (hidden on mobile) -->
      <header class="hidden lg:block bg-white border-b border-slate-100 shadow-sm sticky top-0 z-50">
      <div class="max-w-7xl mx-auto px-3 sm:px-6 py-3 sm:py-6 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-2 sm:gap-4">
          <div class="w-10 h-10 sm:w-12 sm:h-12 bg-gradient-to-br from-slate-600 to-slate-700 rounded-xl flex items-center justify-center shadow-lg">
            <img src="/favicon.svg" alt="FACET" class="h-6 w-6 sm:h-8 sm:w-8" />
          </div>
          <span class="text-2xl sm:text-3xl text-slate-800 tracking-wide font-light">FACET</span>
        </div>
        <div class="flex items-center gap-2 sm:gap-3 flex-wrap">
          <ClientOnly>
            <template #default>
              <NuxtLink
                v-if="currentRole === 'Admin'"
                to="/admin"
                class="px-3 py-1.5 sm:px-4 sm:py-2 bg-orange-50 hover:bg-orange-100 text-orange-700 rounded-lg transition-colors text-xs sm:text-sm font-medium whitespace-nowrap"
              >
                Manage users
              </NuxtLink>
            </template>
          </ClientOnly>

          <NuxtLink to="/policies/create" class="px-3 py-1.5 sm:px-4 sm:py-2 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-lg transition-colors text-xs sm:text-sm font-medium whitespace-nowrap">
            Create policy
          </NuxtLink>
            <div class="min-w-fit sm:min-w-[220px] flex items-center justify-end">
              <div class="flex items-center gap-2 sm:gap-3">
                <div class="flex items-center gap-2">
                  <ClientOnly>
                    <template #default>
                      <div v-if="isAuthenticated" class="flex items-center gap-2">
                        <div class="w-8 h-8 sm:w-9 sm:h-9 rounded-full bg-slate-700 text-white flex items-center justify-center font-medium text-xs sm:text-sm shadow">
                          <span v-if="userInitials">{{ userInitials }}</span>
                          <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 sm:h-5 sm:w-5" viewBox="0 0 20 20" fill="currentColor">
                            <path fill-rule="evenodd" d="M10 2a4 4 0 100 8 4 4 0 000-8zM2 18a8 8 0 1116 0H2z" clip-rule="evenodd" />
                          </svg>
                        </div>
                        <div class="hidden sm:block text-sm text-slate-700">{{ currentUsername || 'You' }}</div>
                        <button @click="logOut" class="px-2 py-1 sm:px-3 sm:py-1 text-xs sm:text-sm text-red-600 border border-red-100 rounded hover:bg-red-50">Logout</button>
                      </div>
                      <div v-else class="w-24" aria-hidden="true"></div>
                    </template>
                  </ClientOnly>
                </div>
              </div>
            </div>
        </div>
      </div>
    </header>

    <div v-if="notice" class="max-w-7xl mx-auto px-3 sm:px-6 mt-4">
      <div class="rounded-md bg-emerald-50 border border-emerald-100 p-3 text-sm sm:text-base text-emerald-800 flex items-center justify-between">
        <div>{{ notice }}</div>
        <button @click="notice = null" class="ml-4 px-2 py-1 sm:px-3 sm:py-1 bg-emerald-100 hover:bg-emerald-200 rounded text-sm">OK</button>
      </div>
    </div>

    <main class="max-w-7xl mx-auto px-3 sm:px-6 py-4 sm:py-8 flex flex-col lg:grid gap-4 sm:gap-6 lg:grid-cols-5">
      <section class="space-y-4 lg:col-span-1 order-1 hidden lg:block">
          <UploadDrop @uploaded="onUploaded" />
          <SensitiveDataBox :types="uniqueTypes" :doc-id="stagedDoc?.id || selectedId" />
          <div class="mt-2 flex flex-col sm:flex-row gap-2 items-stretch sm:items-center">
            <button @click="applySelected" class="px-3 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 active:bg-blue-800 touch-manipulation text-sm sm:text-base" :disabled="!(stagedDoc?.id || selectedId) || selectedCount === 0">
              Apply Changes
            </button>
            <div class="text-xs sm:text-sm text-slate-600 text-center sm:text-left">Selected: {{ selectedCount }}</div>
          </div>
      </section>

      <section class="lg:col-span-3 order-2 lg:order-2">
        <!-- Mobile Upload Area -->
        <div class="lg:hidden mb-4">
          <UploadDrop @uploaded="onUploaded" />
        </div>

        <!-- Mobile: Document Preview Card -->
        <div class="lg:hidden space-y-4">
          <!-- Staged Document -->
          <div v-if="stagedDoc" class="bg-white border border-slate-200 rounded-lg shadow-sm p-4">
            <div class="flex items-start gap-3 mb-3">
              <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-blue-600 rounded-lg flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-sm font-semibold text-slate-900 break-words mb-1">{{ stagedDoc.fileName }}</div>
                <div class="text-xs text-slate-500">
                  {{ new Date(stagedDoc.uploadedAt).toLocaleString() }}
                </div>
                <div class="text-xs text-slate-500">
                  {{ (stagedDoc.sizeBytes / 1024).toFixed(2) }} KB
                </div>
              </div>
            </div>
            <div class="flex gap-2">
              <NuxtLink 
                :to="{ path: '/preview', query: { docId: stagedDoc.id, staged: 'true' } }"
                class="flex-1 px-4 py-2.5 bg-primary text-white rounded-lg hover:bg-primary/90 active:bg-primary/80 text-center text-sm font-medium touch-manipulation transition"
              >
                View & Edit
              </NuxtLink>
              <button 
                @click="discardStaged" 
                class="px-4 py-2.5 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 active:bg-red-200 text-sm font-medium touch-manipulation transition"
              >
                Remove
              </button>
            </div>
          </div>

          <!-- Selected Document -->
          <div v-else-if="selectedDoc" class="bg-white border border-slate-200 rounded-lg shadow-sm p-4">
            <div class="flex items-start gap-3 mb-3">
              <div class="w-12 h-12 bg-gradient-to-br from-slate-600 to-slate-700 rounded-lg flex items-center justify-center flex-shrink-0">
                <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                </svg>
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-sm font-semibold text-slate-900 break-words mb-1">{{ selectedDoc.fileName }}</div>
                <div class="text-xs text-slate-500">
                  {{ new Date(selectedDoc.uploadedAt).toLocaleString() }}
                </div>
                <div class="text-xs text-slate-500">
                  {{ (selectedDoc.sizeBytes / 1024).toFixed(2) }} KB
                </div>
              </div>
            </div>
            <NuxtLink 
              :to="{ path: '/preview', query: { docId: selectedDoc.id } }"
              class="block w-full px-4 py-2.5 bg-primary text-white rounded-lg hover:bg-primary/90 active:bg-primary/80 text-center text-sm font-medium touch-manipulation transition"
            >
              View & Edit
            </NuxtLink>
          </div>

          <!-- No Document Selected -->
          <div v-else class="bg-white border-2 border-dashed border-slate-200 rounded-lg p-8 text-center">
            <svg class="w-16 h-16 mx-auto text-slate-300 mb-3" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <p class="text-slate-500 text-sm mb-1">No document selected</p>
            <p class="text-slate-400 text-xs">Upload a file or select from menu</p>
          </div>

          <!-- Sensitive Data Quick Info -->
          <div v-if="uniqueTypes.length > 0" class="bg-amber-50 border border-amber-200 rounded-lg p-4">
            <div class="flex items-center gap-2 mb-2">
              <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <span class="text-sm font-semibold text-amber-900">{{ uniqueTypes.length }} sensitive data type(s) found</span>
            </div>
            <div class="flex flex-wrap gap-2">
              <span v-for="t in uniqueTypes" :key="t" class="px-2 py-1 bg-amber-100 text-amber-800 rounded text-xs font-medium capitalize">
                {{ t }}
              </span>
            </div>
          </div>
        </div>

        <!-- Desktop: Full Preview -->
        <div class="hidden lg:block">
          <div v-if="stagedDoc" class="mb-3 p-3 sm:p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
            <div class="flex flex-col sm:flex-row justify-between items-start gap-3 sm:gap-0">
              <div class="min-w-0 flex-1">
                <div class="text-sm font-medium text-slate-900 break-words">{{ stagedDoc.fileName }}</div>
                <div class="text-xs text-slate-500 mt-1">
                  {{ new Date(stagedDoc.uploadedAt).toLocaleString() }} • {{ (stagedDoc.sizeBytes / 1024).toFixed(2) }} KB
                </div>
              </div>
              <button @click="discardStaged" class="px-3 py-1.5 bg-red-500 text-white text-xs rounded-md hover:bg-red-600 active:bg-red-700 transition touch-manipulation whitespace-nowrap">
                Remove
              </button>
            </div>
          </div>
          <PreviewPane v-if="stagedDoc || selectedDoc" :url="previewUrl" :contentType="previewType" :documentId="(stagedDoc || selectedDoc)?.id" />
          <div v-else class="card p-6 w-full flex items-center justify-center muted" style="height: 842px; max-height: 80vh;">Nothing to display</div>
        </div>
      </section>

      <section class="lg:col-span-1 order-3 space-y-4 hidden lg:block">
        <DocListBox :docs="docs" :selected-id="selectedId" @select="(id: string) => { selectedId = id }"  @delete="onDelete" class="mb-2"/>
  <PoliciesListBox :policies="policies" :selected-id="selectedPolicyId" @select="selectPolicy"  @view="viewPolicy" @delete="deletePolicy" @use="usePolicy" />
        <div v-if="viewedPolicy" class="mt-2 p-3 border rounded bg-slate-50">
        <div class="font-medium mb-1 text-sm sm:text-base">{{ viewedPolicy.name }}</div>
        <ul class="text-xs sm:text-sm text-slate-600 list-disc pl-5">
          <li v-for="k in activeOptions" :key="k">
            {{ optionLabels[k] || k }}
          </li>
          <li v-if="activeOptions.length === 0">None</li>
        </ul>
        <button
          @click="viewedPolicy = null"
          class="mt-2 px-2 py-1 text-xs text-red-500 border rounded hover:bg-red-50 touch-manipulation"
        >
          Close
        </button>
      </div>
      </section>
    </main>

    <footer class="py-6 sm:py-8 text-center muted text-xs sm:text-sm">FACET 2025</footer>
    </div>
  </ClientOnly>
</template>