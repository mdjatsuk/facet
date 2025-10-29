<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { renderAsync } from 'docx-preview'

const props = defineProps<{ url: string }>()
const container = ref<HTMLElement | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

async function loadDocument() {
  if (!container.value) return
  
  loading.value = true
  error.value = null
  
  try {
    const response = await fetch(props.url)
    if (!response.ok) throw new Error('Failed to load document')
    
    const blob = await response.blob()
    const arrayBuffer = await blob.arrayBuffer()
    
    // Clear previous content
    container.value.innerHTML = ''
    
    // Render the document with high fidelity
    await renderAsync(arrayBuffer, container.value, undefined, {
      className: 'docx-preview',
      inWrapper: true,
      ignoreWidth: false,
      ignoreHeight: false,
      ignoreFonts: false,
      breakPages: true,
      ignoreLastRenderedPageBreak: false,
      experimental: true,
      trimXmlDeclaration: true,
      useBase64URL: false,
      renderChanges: false,
      renderHeaders: true,
      renderFooters: true,
      renderFootnotes: true,
      renderEndnotes: true,
      renderComments: false,
    })
    
    loading.value = false
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load document'
    loading.value = false
  }
}

onMounted(() => {
  loadDocument()
})

watch(() => props.url, () => {
  loadDocument()
})
</script>

<template>
  <div class="word-preview-container w-full h-full overflow-auto bg-gray-100">
    <div v-if="loading" class="flex items-center justify-center h-full">
      <div class="text-center">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
        <p class="text-gray-600">Loading document...</p>
      </div>
    </div>
    
    <div v-else-if="error" class="flex items-center justify-center h-full">
      <div class="text-center text-red-600">
        <p>{{ error }}</p>
      </div>
    </div>
    
    <div 
      ref="container" 
      class="docx-container p-4"
      :class="{ 'hidden': loading || error }"
    ></div>
  </div>
</template>

<style>
.word-preview-container {
  background: #525659;
}

.docx-container {
  max-width: 210mm;
  margin: 0 auto;
}

.docx-preview {
  background: white;
  box-shadow: 0 0 10px rgba(0,0,0,0.3);
  margin-bottom: 20px;
  padding: 96px; /* Standard Word margins */
  min-height: 297mm; /* A4 height */
}

/* Preserve Word document styling */
.docx-preview * {
  font-family: inherit;
  color: inherit;
  font-size: inherit;
  line-height: inherit;
}

.docx-preview table {
  border-collapse: collapse;
  width: 100%;
}

.docx-preview td, .docx-preview th {
  border: 1px solid #ddd;
  padding: 8px;
}
</style>
