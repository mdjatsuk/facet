<script setup lang="ts">

import type { Document } from '../types'



const { get, del } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const docs = ref<Document[]>([])
const selectedId = ref<string | null>(null)
const sensitiveItems = ref<string[]>([])
const policies = ref<Array<{ id: string, name: string, options: Record<string, boolean>, createdAt: string }>>([])

const selectedDoc = computed(() => 
  docs.value.find((d: Document) => d.id === selectedId.value) || docs.value[0] || null
)
const previewUrl = computed(() => selectedDoc.value ? `${apiBase}api/documents/${selectedDoc.value.id}/file` : '')
const previewType = computed(() => selectedDoc.value?.contentType || '')

const route = useRoute()
const notice = ref<string | null>(null)

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
  } else if (selectedId.value && !docs.value.some((d: Document) => d.id === selectedId.value) && docs.value.length > 0) {
    selectedId.value = docs.value[0]?.id || null
  }
}

async function onDelete(id: string) {
  await del(`api/documents/${id}`)
  await refresh() 
}

function onUploaded(doc: Document) {
  refresh(doc.id) 
  selectedId.value = doc.id 
}

function loadPolicies() {
  const stored = localStorage.getItem('policies')
  policies.value = stored ? JSON.parse(stored) : []
}

function onDeletePolicy(id: string) {
  policies.value = policies.value.filter((p: { id: string, name: string, options: Record<string, boolean>, createdAt: string }) => p.id !== id)
  localStorage.setItem('policies', JSON.stringify(policies.value))
}

onMounted(() => {
  refresh()
  loadPolicies()
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
          <SensitiveDataBox :items="sensitiveItems" />
      </section>

      <section class="md:col-span-3">
        <PreviewPane v-if="selectedDoc" :url="previewUrl" :contentType="previewType" />
        <div v-else class="card p-6 w-full aspect-[210/297] max-h-[80vh] flex items-center justify-center muted">Nothing to display</div>
      </section>

      <section class="md:col-span-1 flex flex-col gap-6">
        <DocListBox :docs="docs" :selected-id="selectedId" @select="(id: string) => { selectedId = id }"  @delete="onDelete" />
        <PolicyListBox :policies="policies" @delete="onDeletePolicy" />
      </section>
    </main>

    <footer class="py-8 text-center muted">FACET 2025 </footer>
  </div>
</template>
