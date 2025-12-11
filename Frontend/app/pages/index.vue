<script setup lang="ts">
import { onMounted, watch, computed, ref } from 'vue'
// --- Custom Pattern State ---
const customTypes = ref<string[]>([])

function onPatternFound(type: string) {
  if (!customTypes.value.includes(type)) {
    customTypes.value.push(type)
  }
}
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useDetected } from '../composables/useDetected'
import { useSelection } from '../composables/useSelection'
import type { Document, SensitiveItem, RiskInfo, RiskLevel } from '../types'
// LanguageSwitcher component is auto-registered (Nuxt components)

const { get, del, post } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string
const router = useRouter()
const { t } = useI18n()

const docs = ref<Document[]>([])
const selectedId = ref<string | null>(null)
// Staged upload: shown in preview until Apply Changes creates redacted copy
const stagedDoc = ref<Document | null>(null)

const { setDetected, getDetected, clearDetected, documentDetected } = useDetected()

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

const riskWeights: Record<string, number> = {
  email: 1,
  phone: 1,
  id: 3,
  iban: 3,
  financialinfo: 3,
}

function computeRiskScore(items: SensitiveItem[]): number {
  let score = 0
  for (const item of items || []) {
    const key = (item?.type || '').toLowerCase()
    const weight = riskWeights[key] ?? 1
    score += weight
  }
  return score
}

function levelForScore(score: number): RiskLevel {
  if (score >= 10) return 'high'
  if (score >= 4) return 'medium'
  return 'low'
}

const riskByDoc = computed<Record<string, RiskInfo>>(() => {
  const ids = new Set<string>()
  docs.value.forEach(d => d?.id && ids.add(d.id))
  if (stagedDoc.value?.id) ids.add(stagedDoc.value.id)

  const detectedMap = documentDetected.value
  const result: Record<string, RiskInfo> = {}
  ids.forEach(id => {
    const items = detectedMap.get(id) || []
    const score = computeRiskScore(items)
    result[id] = { score, level: levelForScore(score) }
  })
  return result
})

const route = useRoute()
const notice = ref<string | null>(null)

const { getValues, clear, setAll } = useSelection()
const selectedCount = computed(() => {
  const docId = stagedDoc.value?.id || selectedId.value || ''
  return (getValues(docId) || []).length
})

const viewedPolicy = ref<{ id: string, name: string, options: Record<string, boolean> } | null>(null)

const optionLabels: Record<string, string> = {
  deleteAllEmails: t('policies.create.policyOptions.emails'),
  removePhoneNumbers: t('policies.create.policyOptions.phoneNumbers'),
  removeNationalIds: t('policies.create.policyOptions.nationalIds'),
  anonymizeNames: 'Names',
  removeMailingAddresses: 'Address',
  deleteIPAddresses: 'IP Address',
  removeFinancialInfo: t('policies.create.policyOptions.financialInfo'),
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
    const cached = loadDocsCache()
    const fromApi = await get<Document[]>('documents')
    docs.value = dedupeById([...(fromApi || []), ...cached])
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
  // Ensure uniqueness by id to avoid duplicates
  docs.value = dedupeById(docs.value)
  saveDocsCache(docs.value)
  
  if (selectNewId) {
    selectedId.value = selectNewId
  } else if (!selectedId.value && docs.value.length > 0) {
    // Only set to first doc if no selectedId is currently set
    selectedId.value = docs.value[0]?.id || null
  }
  // Don't override selectedId if it's already set and found in docs
}

function dedupeById(arr: Document[]): Document[] {
  const byId = new Map<string, Document>()
  for (const d of arr) {
    if (d?.id) byId.set(d.id, d)
  }
  return Array.from(byId.values())
}

function viewPolicy(id: string) {
  viewedPolicy.value = policies.value.find(p => p.id === id) || null
}

async function onDelete(id: string) {
  try {
    await del(`documents/${id}`)
  } catch (e: any) {
    const status = e?.response?.status || e?.status
    if (status !== 404) {
      console.warn('Delete failed', e)
    }
    // Treat 404 as already deleted; continue cleanup
  }
  // Local cleanup regardless of server outcome
  clearDetected(id)
  if (!isAuthenticated.value) {
    removeAnonDoc(id)
  }
  // Remove from list and persist cache
  docs.value = docs.value.filter(d => d.id !== id)
  saveDocsCache(docs.value)
  // Adjust selection if needed
  if (selectedId.value === id) {
    selectedId.value = docs.value[0]?.id || null
  }
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

const selectedIdKey = computed(() => `selectedId_${currentUsername.value ?? 'anon'}`)

// Persist selectedId to localStorage
watch(selectedId, (newVal) => {
  console.log('Selected doc changed to:', selectedId.value)
  if (newVal) {
    localStorage.setItem(selectedIdKey.value, newVal)
  } else {
    localStorage.removeItem(selectedIdKey.value)
  }
})

// Scope stagedDoc and policies in localStorage per-user so different accounts don't see each other's data
const { currentUsername, isAuthenticated, logOut, currentRole } = useAuth()
const { locale } = useI18n()

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
const docsKey = computed(() => `docs_${currentUsername.value ?? 'anon'}`)

function saveDocsCache(list: Document[]) {
  try {
    localStorage.setItem(docsKey.value, JSON.stringify(list))
  } catch (e) {
    console.warn('Failed to cache docs', e)
  }
}

function loadDocsCache(): Document[] {
  try {
    const raw = localStorage.getItem(docsKey.value)
    if (!raw) return []
    const parsed = JSON.parse(raw) as Document[]
    return Array.isArray(parsed) ? parsed : []
  } catch (e) {
    console.warn('Failed to read docs cache', e)
    return []
  }
}

// Persist stagedDoc to localStorage
watch(stagedDoc, (newVal) => {
  if (newVal) {
    localStorage.setItem(stagedDocKey.value, JSON.stringify(newVal))
  } else {
    localStorage.removeItem(stagedDocKey.value)
  }
}, { deep: true })

watch(docs, (val) => {
  saveDocsCache(dedupeById(val))
}, { deep: true })

// Restore stagedDoc from localStorage on mount
onMounted(() => {
  const cachedDocs = loadDocsCache()
  if (cachedDocs.length > 0 && docs.value.length === 0) {
    docs.value = dedupeById(cachedDocs)
  }
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

// Listen for realtime redaction completion events to update list without reload
onMounted(() => {
  const handler = (e: Event) => {
    const detail = (e as CustomEvent).detail as { newDoc: Document, detectedItems?: SensitiveItem[], oldDocId?: string }
    if (!detail?.newDoc?.id) return
    const parsedDocId = detail.newDoc.id
    const oldDocId = detail.oldDocId
    const detectedItems = detail.detectedItems || []

    // Remove old doc if provided and different
    if (oldDocId && oldDocId !== parsedDocId) {
      docs.value = docs.value.filter(d => d.id !== oldDocId)
    }
    // Insert or replace new doc
    const existingIdx = docs.value.findIndex(d => d.id === parsedDocId)
    if (existingIdx === -1) {
      docs.value.unshift(detail.newDoc)
    } else {
      docs.value[existingIdx] = detail.newDoc
    }
    // Set detected items for the new doc
    if (detectedItems.length > 0) {
      setDetected(parsedDocId, detectedItems)
    }
    // Ensure uniqueness and select it
    docs.value = dedupeById(docs.value)
    selectedId.value = parsedDocId
  }
  window.addEventListener('facet:new-redacted-doc', handler as EventListener)
  // Cleanup on unmount
  onUnmounted(() => window.removeEventListener('facet:new-redacted-doc', handler as EventListener))
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
  
  // Check if we have docId in query (e.g., after redacting on mobile)
  const initialDocId = (route.query.docId as string) || undefined
  const fromSensitive = route.query.fromSensitive === 'true'
  
  // Special handling for direct docId navigation (mobile redact flow only)
  // Only use query docId if we're clearly coming from mobile (no fromSensitive flag)
  if (initialDocId && !fromSensitive && !docs.value.length) {
    console.log('[onMounted] Direct docId in query (mobile):', initialDocId)
    selectedId.value = initialDocId
    // Load full document list and select this one so previous
    // redacted documents remain visible in the list.
    refresh(initialDocId)
    // Clean up query parameter once refresh is triggered
    router.replace({ path: '/', query: {} })

    return
  } else {
    // Restore selectedId from localStorage (normal flow)
    const savedSelectedId = localStorage.getItem(selectedIdKey.value)
    if (savedSelectedId) {
      selectedId.value = savedSelectedId
      console.log('[onMounted] Restored selectedId from localStorage:', savedSelectedId)
    }
  }
  
  if (fromSensitive && initialDocId) {
    console.log('[onMounted] Returning from sensitive page with docId:', initialDocId)
    
    // Restore detected items if stored
    const storedDetected = sessionStorage.getItem('detectedItemsOnReturn')
    if (storedDetected) {
      try {
        const parsed = JSON.parse(storedDetected)
        const { docId: storedDocId, items } = parsed
        
        if (storedDocId === initialDocId && items && Array.isArray(items) && items.length > 0) {
          setDetected(initialDocId, items)
          console.log('[onMounted] Restored detected items:', items.length)
        }
      } catch (e) {
        console.error('Failed to parse detectedItemsOnReturn', e)
      } finally {
        sessionStorage.removeItem('detectedItemsOnReturn')
      }
    }
    // Refresh the full document list and select this document
    // instead of replacing the list with a single item.
    refresh(initialDocId)
    // Clean up query parameter
    router.replace({ path: '/', query: {} })

    return
  }
  
  // Check if returning from sensitive detail page via sessionStorage (old method)
  const returnToDocId = sessionStorage.getItem('returnToDocId')
  if (returnToDocId) {
    sessionStorage.removeItem('returnToDocId')
    
    // Check if detected items were also stored
    const storedDetected = sessionStorage.getItem('detectedItemsOnReturn')
    if (storedDetected) {
      try {
        const parsed = JSON.parse(storedDetected)
        const { docId: storedDocId, items } = parsed
        
        // Verify the data is fresh and matches the current docId
        if (storedDocId === returnToDocId && items && Array.isArray(items) && items.length > 0) {
          // Restore detected items
          setDetected(returnToDocId, items)
        }
      } catch (e) {
        console.error('Failed to parse detectedItemsOnReturn', e)
      } finally {
        // Always clean up, even if parsing failed
        sessionStorage.removeItem('detectedItemsOnReturn')
      }
    }
    
    // Check if the document is already loaded and selected
    if (selectedId.value === returnToDocId && docs.value.find(d => d.id === returnToDocId)) {
      // Document is already selected, just restore detected items (already done above)
      console.log('Document already selected, skipping refresh')
      return
    }
    
    // Refresh to get the document list and select the returned docId
    refresh(returnToDocId)
    return
  }
  
  // If not returning from sensitive page, do normal refresh
  if (!fromSensitive) {
    const fallbackDocId = (route.query.docId as string) || undefined
    refresh(fallbackDocId)
    // After refresh, verify that selectedId is still valid
    // If it's set but document not found, it will be reset by refresh()
    // But we want to keep it if document exists
    if (selectedId.value && docs.value.length >= 0) {
      if (!docs.value.some(d => d.id === selectedId.value)) {
        // Document not found in list, try to load it from server
        console.log('[onMounted] selectedId document not in list, loading from server:', selectedId.value)
        get<Document>(`documents/${selectedId.value}`)
          .then(doc => {
            if (doc) {
              docs.value.unshift(doc)
              console.log('[onMounted] Loaded missing document:', selectedId.value)
            }
          })
          .catch(e => {
            console.error('[onMounted] Failed to load document:', selectedId.value, e)
            // Document doesn't exist on server, clear it from localStorage
            if (e?.response?.status === 404 || e?.data?.includes('404')) {
              console.log('[onMounted] Document deleted, clearing from localStorage')
              localStorage.removeItem(selectedIdKey.value)
              selectedId.value = docs.value[0]?.id || null
            }
          })
      }
    }
  }
})

// Watch for route query changes (e.g., when returning from preview with new docId)
watch(() => route.query.docId, async (newDocId) => {
  if (newDocId) {
    // Check if returning from sensitive detail page
    const fromSensitive = route.query.fromSensitive === 'true'
    
    if (fromSensitive) {
      // Restore detected items if stored
      const storedDetected = sessionStorage.getItem('detectedItemsOnReturn')
      if (storedDetected) {
        try {
          const parsed = JSON.parse(storedDetected)
          const { docId: storedDocId, items } = parsed
          
          if (storedDocId === newDocId && items && Array.isArray(items) && items.length > 0) {
            setDetected(newDocId as string, items)
          }
        } catch (e) {
          console.error('Failed to parse detectedItemsOnReturn', e)
        } finally {
          sessionStorage.removeItem('detectedItemsOnReturn')
        }
      }
      
      // Load the document if it's not in the docs array
      if (!docs.value.find(d => d.id === newDocId)) {
        try {
          const doc = await get<Document>(`documents/${newDocId}`)
          if (doc && !docs.value.find(d => d.id === newDocId)) {
            docs.value.unshift(doc)
          }
        } catch (e) {
          console.error('Failed to load document:', e)
        }
      }
      
      // Ensure the document is selected
      if (selectedId.value !== newDocId) {
        selectedId.value = newDocId as string
      }
      
      // Clean up the query parameter AFTER setting selectedId
      router.replace({ path: '/', query: {} })
      return
    }
    
    // Check if there's a new redacted document in sessionStorage (from preview.vue or sensitive-data.vue)
    const storedNewDoc = sessionStorage.getItem('newRedactedDoc')
    if (storedNewDoc) {
      try {
        const { newDoc, detectedItems, oldDocId } = JSON.parse(storedNewDoc)
        sessionStorage.removeItem('newRedactedDoc')
        
        const parsedDocId = newDoc?.id
        if (!parsedDocId) {
          throw new Error('No document ID in newRedactedDoc')
        }
        
        // If an old doc id is provided, remove it to avoid duplicates
        if (oldDocId) {
          docs.value = docs.value.filter(d => d.id !== oldDocId)
        }
        // Add or update the new doc in the list
        const existingIdx = docs.value.findIndex(d => d.id === parsedDocId)
        if (existingIdx === -1) {
          docs.value.unshift(newDoc)
        } else {
          docs.value[existingIdx] = newDoc
        }
        // Final guard: ensure no duplicates remain
        docs.value = dedupeById(docs.value)
        
        // Set detected items
        if (detectedItems && detectedItems.length > 0) {
          setDetected(parsedDocId, detectedItems)
        }
        
        // Select the new document
        selectedId.value = parsedDocId
        console.log('Restored redacted doc from sessionStorage:', parsedDocId)
      } catch (e) {
        console.error('Failed to parse newRedactedDoc from sessionStorage', e)
        sessionStorage.removeItem('newRedactedDoc')
        refresh(newDocId as string)
      }
    } else {
      // Normal refresh if no sessionStorage data
      refresh(newDocId as string)
    }
  }
})

// Reload policies when currentUsername changes (login/logout/switch accounts)
watch(currentUsername, () => {
  try {
    policies.value = JSON.parse(localStorage.getItem(policiesKey.value) || '[]')
  } catch (e) {
    policies.value = []
  }
  
  // Also restore selectedId when username changes
  const savedSelectedId = localStorage.getItem(selectedIdKey.value)
  if (savedSelectedId) {
    selectedId.value = savedSelectedId
    console.log('[currentUsername watch] Restored selectedId:', savedSelectedId)
  }

  const cachedDocs = loadDocsCache()
  if (cachedDocs.length > 0) {
    docs.value = dedupeById(cachedDocs)
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
    
    if (!newDocId || !newDoc) {
      throw new Error('No document ID returned from server')
    }
    
    // Clear old document data and selections
    clearDetected(targetId)
    clear(targetId)
    
    // Set detected items for new redacted document
    if (detectedItems.length > 0) {
      setDetected(newDocId, detectedItems)
    }
    
    // Add or update the document in the list
    const existingIdx = docs.value.findIndex(d => d.id === newDocId)
    if (existingIdx === -1) {
      docs.value.unshift(newDoc)
    } else {
      docs.value[existingIdx] = newDoc
    }
    
    // Remove the old document from docs list only if id changed
    if (newDocId !== targetId) {
      docs.value = docs.value.filter(d => d.id !== targetId)
    }
    saveDocsCache(docs.value)
    
    // If this was a staged document, clear it immediately
    if (stagedDoc.value?.id === targetId) {
      stagedDoc.value = null
      localStorage.removeItem(stagedDocKey.value)
    }
    
    // Clear the old document from localStorage if it was the selected one
    if (selectedIdKey.value && localStorage.getItem(selectedIdKey.value) === targetId) {
      localStorage.removeItem(selectedIdKey.value)
    }
    
    // For anonymous users, save the new document ID to localStorage
    if (!isAuthenticated.value && newDocId) {
      saveAnonDoc(newDocId)
      removeAnonDoc(targetId)
    }
    
    // Select the new document
    selectedId.value = newDocId
    
    console.log('Applied changes: new doc', newDocId, 'is now selected')
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

// Anonymous document helpers
function saveAnonDoc(id: string) {
  const key = 'anonDocs'
  try {
    const current = JSON.parse(localStorage.getItem(key) || '[]') as string[]
    if (!current.includes(id)) {
      current.unshift(id)
      localStorage.setItem(key, JSON.stringify(current))
    }
  } catch (e) {
    localStorage.setItem(key, JSON.stringify([id]))
  }
}

function removeAnonDoc(id: string) {
  const key = 'anonDocs'
  try {
    const current = JSON.parse(localStorage.getItem(key) || '[]') as string[]
    const next = current.filter(x => x !== id)
    localStorage.setItem(key, JSON.stringify(next))
  } catch (e) {
    // If parsing fails, just overwrite with empty
    localStorage.setItem(key, JSON.stringify([]))
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
        :risk-by-doc="riskByDoc"
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
                {{ $t('nav.manageUsers') }}
              </NuxtLink>
            </template>
          </ClientOnly>

          <NuxtLink to="/policies/create" class="px-3 py-1.5 sm:px-4 sm:py-2 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-lg transition-colors text-xs sm:text-sm font-medium whitespace-nowrap">
            {{ $t('nav.createPolicy') }}
          </NuxtLink>
          
          <LanguageSwitcher class="min-w-fit" />
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
                        <button @click="logOut" class="px-2 py-1 sm:px-3 sm:py-1 text-xs sm:text-sm text-red-600 border border-red-100 rounded hover:bg-red-50">{{ $t('nav.logout') }}</button>
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

    <!-- PAGE LAYOUT -->
    <!-- Mobile Version: fully isolated block for clarity -->
    <main class="max-w-7xl mx-auto px-3 sm:px-6 py-4 sm:py-8">
      <!-- MOBILE SECTION (visible below lg breakpoint) -->
      <section class="lg:hidden space-y-4" aria-label="Mobile Home Section">
        <MobileHome
          :stagedDoc="stagedDoc"
          :selectedDoc="selectedDoc as any"
          :uniqueTypes="uniqueTypes"
          :riskByDoc="riskByDoc"
          @uploaded="onUploaded"
          @discard-staged="discardStaged"
        />
      </section>

      <!-- DESKTOP SECTION (visible at and above lg breakpoint) -->
      <section class="hidden lg:block" aria-label="Desktop Home Section">
        <DesktopHome
          :docs="docs"
          :policies="policies"
          :selectedId="selectedId"
          :selectedPolicyId="selectedPolicyId"
          :stagedDoc="stagedDoc"
          :uniqueTypes="uniqueTypes"
          :selectedCount="selectedCount"
          :previewUrl="previewUrl"
          :previewType="previewType"
          :documentId="(stagedDoc || selectedDoc)?.id"
          :viewedPolicy="viewedPolicy"
          :activeOptions="activeOptions"
          :optionLabels="optionLabels"
          :riskByDoc="riskByDoc"
          @uploaded="onUploaded"
          @apply-selected="applySelected"
          @discard-staged="discardStaged"
          @select-doc="(id: string) => { selectedId = id }"
          @delete-doc="onDelete"
          @select-policy="selectPolicy"
          @view-policy="viewPolicy"
          @delete-policy="deletePolicy"
          @use-policy="() => usePolicy()"
          @close-viewed-policy="() => { viewedPolicy = null }"
          @pattern-found="onPatternFound"
        />
      </section>
    </main>

    <footer class="py-6 sm:py-8 text-center muted text-xs sm:text-sm">FACET 2025</footer>
    </div>
  </ClientOnly>
</template>