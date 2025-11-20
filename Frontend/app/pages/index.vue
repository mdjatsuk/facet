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
  docs.value = await get<Document[]>('documents')
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
    const docId = res?.document?.id || res?.Document?.id
    if (docId) await refresh(docId)
    else await refresh()
  }
  catch (e) {
    console.error('Save redact failed', e)
    alert('Failed to create redacted copy')
    return
  }

  if (stagedDoc.value?.id === targetId) {
    stagedDoc.value = null
    clearDetected(targetId)
    clear(targetId)
  } else {

    clear(selectedId.value)
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
  catch (e) {
    console.error('Failed to discard staged document', e)
    alert('Failed to discard staged document')
  }
}

</script>

<template>
  <!-- Render only on client to avoid server rendering protected page before auth is ready -->
  <ClientOnly>
    <div class="min-h-screen">
      <header class="bg-white border-b border-slate-100 shadow-sm sticky top-0 z-50">
      <div class="max-w-7xl mx-auto px-3 sm:px-6 py-3 sm:py-6 flex flex-wrap items-center justify-between gap-3">
        <div class="flex items-center gap-2 sm:gap-4">
          <div class="w-10 h-10 sm:w-12 sm:h-12 bg-gradient-to-br from-slate-600 to-slate-700 rounded-xl flex items-center justify-center shadow-lg">
            <img src="/favicon.svg" alt="FACET" class="h-6 w-6 sm:h-8 sm:w-8" />
          </div>
          <span class="text-2xl sm:text-3xl text-slate-800 tracking-wide font-light">FACET</span>
        </div>
        <div class="flex items-center gap-2 sm:gap-3 flex-wrap">
          <!-- Admin-only: Set Roles button placed before Create new policy -->
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
            <!-- Stable container so server/client structure (classes) match to avoid hydration class mismatch -->
            <div class="min-w-fit sm:min-w-[220px] flex items-center justify-end">
              <div class="flex items-center gap-2 sm:gap-3">
                <!-- This container always has the same classes on server and client -->
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
      <section class="space-y-4 lg:col-span-1 order-1">
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
        <div v-else class="card p-6 w-full aspect-[210/297] max-h-[60vh] sm:max-h-[80vh] flex items-center justify-center muted">Nothing to display</div>
      </section>

      <section class="lg:col-span-1 order-3 space-y-4">
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