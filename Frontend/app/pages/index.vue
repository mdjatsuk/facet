<script setup lang="ts">

type DocItem = { id:string; fileName:string; contentType:string; sizeBytes:number; uploadedAt:string }
const { get,del } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const docs = ref<DocItem[]>([])
const selectedId = ref<string|null>(null)
const highlightedId = ref<string|null>(null)
const selectedDoc = computed(() => docs.value.find(d => d.id === selectedId.value) || docs.value[0])
const previewUrl = computed(() => selectedDoc.value ? `${apiBase}/api/documents/${selectedDoc.value.id}/file` : '')
const previewType = computed(() => selectedDoc.value?.contentType || '')

async function refresh(selectNewId?: string){
  docs.value = await get<DocItem[]>('/api/documents')
  if (selectNewId) {
    selectedId.value = selectNewId
  } else if (!selectedId.value && docs.value.length > 0) {
    selectedId.value = docs.value[0].id
  } else if (selectedId.value && !docs.value.some(d => d.id === selectedId.value) && docs.value.length > 0) {
    selectedId.value = docs.value[0].id
  }
}

async function onDelete(id: string) {
  await del(`/api/documents/${id}`)
  await refresh() 
}

function onUploaded(doc:any)
{ refresh(doc.id) 
selectedId.value = doc.id 
}

onMounted(() => refresh())
</script>
<template>
  <div class="min-h-screen">
    <header class="sticky top-0 z-10 bg-white/70 backdrop-blur border-b border-slate-200">
      <div class="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
        <div class="flex items-center gap-3">
          <img src="/favicon.svg" alt="FACET" class="h-6 w-6" />
          <span class="font-semibold tracking-tight">FACET</span>
        </div>
        <div class="muted">Upload → Preview → Open</div>
      </div>
    </header>

    <main class="max-w-7xl mx-auto px-6 py-8 grid gap-6 md:grid-cols-3">
      <section class="space-y-4 md:col-span-1">
        <div class="card p-6">
          <h2 class="text-xl font-semibold mb-1">Laadi üles dokument</h2>
          <p class="muted mb-4">PDF või pildifail (JPG/PNG). Salvestatakse lokaalselt demo jaoks.</p>
          <UploadDrop @uploaded="onUploaded" />
        </div>
      </section>

      <section class="md:col-span-1">
        <PreviewPane v-if="selectedDoc" :url="previewUrl" :contentType="previewType" />
        <div v-else class="card p-6 h-[600px] flex items-center justify-center muted">Pole midagi näidata</div>
      </section>

      <section class="md:col-span-1">
        <DocListBox :docs="docs.value" :selected-id="highlightedId" @select="(id: string) => { selectedId = id; highlightedId = id }"  @highlight="(id: string) => highlightedId = id"  @delete="onDelete" />
      </section>
    </main>

    <footer class="py-8 text-center muted">FACET • Nuxt 4 + Tailwind • SQLite persist</footer>
  </div>
</template>
