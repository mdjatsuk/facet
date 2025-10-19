<script setup lang="ts">
import { useDetected } from '../composables/useDetected'
import type { Document, SensitiveItem } from '../types'  

const { get, del } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const docs = ref<Document[]>([])
const selectedId = ref<string | null>(null)

const { setDetected, getDetected, clearDetected } = useDetected()

const policies = ref<{ id: string, name: string, options: Record<string, boolean> }[]>([])

const selectedDoc = computed(() => 
  docs.value.find(d => d.id === selectedId.value) || docs.value[0] || null
)
const previewUrl = computed(() => selectedDoc.value ? `${apiBase}api/documents/${selectedDoc.value.id}/file` : '')
const previewType = computed(() => selectedDoc.value?.contentType || '')

const currentDetected = computed(() => getDetected(selectedId.value || '') || [])
const uniqueTypes = computed(() => [...new Set(currentDetected.value.map(d => d.type))])

const route = useRoute()
const notice = ref<string | null>(null)

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

watchEffect(() => {
  if (route.query.notice === 'policy-created') {
    notice.value = 'A new policy has been added to your list'
  }
})

async function refresh(selectNewId?: string) {
  docs.value = await get<Document[]>('api/documents')
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
  await del(`api/documents/${id}`)
  clearDetected(id) 
  await refresh() 
}

function onUploaded(payload: { document: Document, detected?: SensitiveItem[] }) {
  console.log('Upload response payload:', payload)
  console.log('Detected items:', payload.detected)
  setDetected(payload.document.id, payload.detected || [])  
  refresh(payload.document.id)
  selectedId.value = payload.document.id
  console.log('Detected for doc:', payload.document.id, payload.detected)
}

watch(selectedId, () => {
  console.log('Selected doc changed to:', selectedId.value)
})

function deletePolicy(id: string) {
  policies.value = policies.value.filter(p => p.id !== id)
  localStorage.setItem('policies', JSON.stringify(policies.value))
}

function selectPolicy(id: string) {
  console.log('Selected policy:', id)
}

onMounted(() => 
{
  policies.value = JSON.parse(localStorage.getItem('policies') || '[]')
    refresh()
})

</script>

<template>
  <div class="min-h-screen">
    <header class="bg-white border-b border-slate-100 shadow-sm">
      <div class="max-w-7xl mx-auto px-6 py-6 flex items-center justify-between">
        <div class="flex items-center gap-4">
          <div class="w-12 h-12 bg-gradient-to-br from-slate-600 to-slate-700 rounded-xl flex items-center justify-center shadow-lg">
            <img src="/favicon.svg" alt="FACET" class="h-8 w-8" />
          </div>
          <span class="text-3xl text-slate-800 tracking-wide font-light">FACET</span>
        </div>
        <div class="flex items-center gap-3">
          <NuxtLink to="/policies/create" class="px-4 py-2 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 rounded-lg transition-colors text-sm font-medium">
            Create new policy
          </NuxtLink>
          <button class="px-4 py-2 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded-lg transition-colors text-sm font-medium">
            Sign In
          </button>
        </div>
      </div>
    </header>

    <div v-if="notice" class="max-w-7xl mx-auto px-6 mt-4">
      <div class="rounded-md bg-emerald-50 border border-emerald-100 p-3 text-emerald-800 flex items-center justify-between">
        <div>{{ notice }}</div>
        <button @click="notice = null" class="ml-4 px-3 py-1 bg-emerald-100 hover:bg-emerald-200 rounded">OK</button>
      </div>
    </div>

    <main class="max-w-7xl mx-auto px-6 py-8 grid gap-6 md:grid-cols-5">
      <section class="space-y-4 md:col-span-1">
          <UploadDrop @uploaded="onUploaded" />
          <SensitiveDataBox :types="uniqueTypes" :doc-id="selectedId" />
      </section>

      <section class="md:col-span-3">
        <PreviewPane v-if="selectedDoc" :url="previewUrl" :contentType="previewType" />
        <div v-else class="card p-6 w-full aspect-[210/297] max-h-[80vh] flex items-center justify-center muted">Nothing to display</div>
      </section>

      <section class="md:col-span-1">
        <DocListBox :docs="docs" :selected-id="selectedId" @select="(id: string) => { selectedId = id }"  @delete="onDelete" class="mb-2"/>
        <PoliciesListBox :policies="policies" :selected-id="selectedId" @select="selectPolicy"  @view="viewPolicy" @delete="deletePolicy" />
        <div v-if="viewedPolicy" class="mt-2 p-3 border rounded bg-slate-50">
        <div class="font-medium mb-1">{{ viewedPolicy.name }}</div>
        <ul class="text-sm text-slate-600 list-disc pl-5">
          <li v-for="k in activeOptions" :key="k">
            {{ optionLabels[k] || k }}
          </li>
          <li v-if="activeOptions.length === 0">None</li>
        </ul>
        <button
          @click="viewedPolicy = null"
          class="mt-2 px-2 py-1 text-xs text-red-500 border rounded hover:bg-red-50"
        >
          Close
        </button>
      </div>
      </section>
    </main>

    <footer class="py-8 text-center muted">FACET 2025 </footer>
  </div>
</template>