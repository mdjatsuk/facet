<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { renderAsync } from 'docx-preview'

const props = defineProps<{ url: string, fullScreen?: boolean }>()
const container = ref<HTMLElement | null>(null)
const outerContainer = ref<HTMLElement | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)

function centerPreview() {
  const outer = outerContainer.value
  if (!outer) return

  // Center horizontally, keep top aligned
  const maxScrollLeft = outer.scrollWidth - outer.clientWidth
  if (maxScrollLeft > 0) {
    outer.scrollLeft = maxScrollLeft / 2
  }
  outer.scrollTop = 0
}

async function loadDocument() {
  if (!container.value) return
  
  loading.value = true
  error.value = null
  
  const maxAttempts = props.fullScreen ? 8 : 1
  let attempt = 0
  
  while (attempt < maxAttempts) {
    try {
      const response = await fetch(props.url)
      if (!response.ok) {
        // On mobile fullScreen, retry a few times on 404
        if (props.fullScreen && response.status === 404) {
          attempt++
          if (attempt >= maxAttempts) {
            console.error(`[WordPreview] File not found after ${attempt} attempts: ${props.url}`)
            error.value = 'Document file not found after retries'
            loading.value = false
            return
          }
          // Increase delay based on attempt number (1000ms, 1500ms, 2000ms, ...)
          const delayMs = 1000 + (attempt * 500)
          console.log(`[WordPreview] Retry attempt ${attempt}/${maxAttempts}, waiting ${delayMs}ms before retry`)
          await new Promise(r => setTimeout(r, delayMs))
          continue
        }
        throw new Error('Failed to load document')
      }
      
      const blob = await response.blob()
      const arrayBuffer = await blob.arrayBuffer()
      
      // Verify container still exists after async operations
      if (!container.value) {
        console.warn('[WordPreview] Container was removed during loading')
        return
      }
      
      // Clear previous content
      container.value.innerHTML = ''
      
      // Render the document with high fidelity
      if (!container.value) {
        console.warn('[WordPreview] Container was removed before rendering')
        return
      }
      
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
      centerPreview()
      return
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to load document'
      loading.value = false
      console.error('[WordPreview] Error loading document:', e)
      return
    }
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
  <div ref="outerContainer" class="word-preview-container w-full h-full bg-gray-100">
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
      :class="[
        fullScreen ? 'docx-container-mobile' : 'docx-container',
        { 'hidden': loading || error }
      ]"
    ></div>
  </div>
</template>

<style>
.word-preview-container {
  background: #525659;
  overflow-x: auto !important;
  overflow-y: auto !important;
  -webkit-overflow-scrolling: touch;
  position: relative;
}

.docx-container {
  padding: 1rem;
  width: max-content;
  min-width: 100%;
}

.docx-container-mobile {
  padding: 0.5rem 0.5rem 6rem 0.5rem; /* Extra bottom padding for mobile nav bar */
  width: 100%;
  min-height: 100%;
}

.docx-preview {
  background: white;
  box-shadow: 0 0 10px rgba(0,0,0,0.3);
  margin: 0 auto 20px auto;
  padding: 25.4mm; /* 1 inch margins = 25.4mm */
  width: 210mm; /* A4 width */
  min-height: 297mm; /* A4 height */
  box-sizing: border-box;
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
  word-wrap: break-word;
}

/* Ensure images don't overflow */
.docx-preview img {
  max-width: 100%;
  height: auto;
}

/* Mobile: keep A4 format, allow horizontal scroll, add bottom padding */
@media (max-width: 640px) {
  .word-preview-container {
    touch-action: pan-x pan-y;
    overflow-x: auto !important;
    overflow-y: auto !important;
  }
  
  .docx-container-mobile {
    padding: 0.5rem 0.5rem 8rem 0.5rem; /* Extra bottom padding for mobile nav bar */
    width: fit-content;
    min-width: 100%;
  }
  
  .docx-container-mobile .docx-preview {
    width: 210mm !important; /* Keep A4 width on mobile */
    min-height: 297mm !important; /* Keep A4 height on mobile */
    padding: 25.4mm !important; /* Keep standard margins */
    margin: 0 auto 2rem auto; /* Add bottom margin */
  }
}
</style>
