<script setup lang="ts">
import type { Document } from '~/types'

const { get, del } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const docs = ref<Document[]>([])
const selectedId = ref<string | null>(null)
const highlightedId = ref<string | null>(null)
const selectedDoc = computed(() => 
  docs.value.find(d => d.id === selectedId.value) || docs.value[0] || null
)
const previewUrl = computed(() => selectedDoc.value ? `${apiBase}api/documents/${selectedDoc.value.id}/file` : '')
const previewType = computed(() => selectedDoc.value?.contentType || '')

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

async function onDelete(id: string) {
  await del(`api/documents/${id}`)
  await refresh() 
}

function onUploaded(doc: Document) {
  refresh(doc.id) 
  selectedId.value = doc.id 
}

onMounted(() => refresh())
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
        <button class="px-4 py-2 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded-lg transition-colors text-sm font-medium">
          Sign In
        </button>
      </div>
    </header>

    <main class="max-w-7xl mx-auto px-6 py-8 grid gap-6 md:grid-cols-5">
      <section class="space-y-4 md:col-span-1">
        <div class="card p-6">
          <h2 class="text-xl font-semibold mb-1">Upload Document</h2>
          <p class="muted mb-4">PDF or image file (JPG/PNG).</p>
          <UploadDrop @uploaded="onUploaded" />
        </div>
      </section>

      <section class="md:col-span-3">
        <PreviewPane v-if="selectedDoc" :url="previewUrl" :contentType="previewType" />
        <div v-else class="card p-6 w-full aspect-[210/297] max-h-[80vh] flex items-center justify-center muted">Nothing to display</div>
      </section>

      <section class="md:col-span-1">
        <DocListBox :docs="docs" :selected-id="selectedId" @select="(id: string) => { selectedId = id }"  @delete="onDelete" />
      </section>
    </main>

    <footer class="py-8 text-center muted">FACET 2025 </footer>
  </div>
</template>
