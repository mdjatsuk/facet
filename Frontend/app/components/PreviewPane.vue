<script setup lang="ts">
import { computed, ref, watch, onMounted, onBeforeUnmount } from 'vue'
import { useI18n } from 'vue-i18n'
const { t } = useI18n()
const props = defineProps<{ url: string, contentType?: string, documentId?: string, fullScreen?: boolean }>()

const config = useRuntimeConfig()
const apiBase = config.public.apiBase as string

const isZoomed = ref(false)

const isDoc = computed(() =>
  /(msword|wordprocessingml)/i.test(props.contentType || '') ||
  /\.(doc|docx)$/i.test(props.url)
)
const isText = computed(() =>
  (props.contentType || '').startsWith('text/') ||
  /\.(txt)$/i.test(props.url)
)


const objectUrl = ref<string | null>(null)
const textContent = ref<string>('')
const tokenState = useState<string | undefined>('token')

async function loadObjectUrl() {
  if (objectUrl.value) {
    URL.revokeObjectURL(objectUrl.value)
    objectUrl.value = null
  }

  // Clear text content as well
  textContent.value = ''

  if (!tokenState.value) return
  if (!props.url || (!props.documentId && !props.url.startsWith(apiBase))) return

  try {
    const headers: Record<string,string> = { Authorization: `Bearer ${tokenState.value}` }
    const resp = await fetch(props.url, { method: 'GET', headers })
    if (!resp.ok) {
      console.warn('Preview fetch failed', resp.status)
      return
    }
    const blob = await resp.blob()
    objectUrl.value = URL.createObjectURL(blob)
    
    // Load text content for text files
    if (isText.value) {
      try {
        textContent.value = await blob.text()
      } catch (e) {
        console.error('Failed to read text content', e)
        textContent.value = 'Failed to load text content'
      }
    }
  } catch (e) {
    console.error('Failed to fetch preview with token', e)
  }
}

watch(() => [props.url, props.documentId, tokenState.value], () => {
  loadObjectUrl()
}, { deep: true })

onMounted(() => loadObjectUrl())
onBeforeUnmount(() => {
  if (objectUrl.value) URL.revokeObjectURL(objectUrl.value)
})

const previewUrl = computed(() => {
  if (isDoc.value) return objectUrl.value || props.url
  return props.url
})

const textPreviewUrl = computed(() => {
  if (isText.value) return objectUrl.value || props.url
  return props.url
})

async function downloadPreview() {
  try {
    const url = previewUrl.value
    const resp = await fetch(url)
    if (!resp.ok) throw new Error('Failed to fetch file')
    const blob = await resp.blob()

    let filename: string = ''
    if (props.documentId) {
      try {
        const metaResp = await fetch(`${apiBase}/documents/${props.documentId}`)
        if (metaResp.ok) {
          const meta = await metaResp.json()
          if (meta && meta.fileName) filename = meta.fileName as string
          if (meta && meta.FileName) filename = meta.FileName as string
        }
      } catch (e) {
      }
    }

    if (!filename) {
      const cd = resp.headers.get('content-disposition') || ''
      const fnameMatch = /filename\*=UTF-8''([^;\n\r]+)/i.exec(cd)
      if (fnameMatch && fnameMatch[1]) {
        filename = decodeURIComponent(fnameMatch[1])
      } else {
        const m2 = /filename="?([^";]+)"?/i.exec(cd)
        if (m2 && m2[1]) filename = m2[1]
      }
    }

    if (!filename) {
      try {
        const u = new URL(url, window.location.href)
        filename = u.pathname.split('/').pop() || 'file'
      } catch (e) {
        const parts = url.split('/')
        const last = String(parts[parts.length - 1] ?? 'file')
        filename = last.split('?')[0] ?? 'file'
      }
    }

    filename = filename.replace(/[\r\n\0"\\]/g, '') || 'file'

  const downloadName = filename

    const objUrl = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = objUrl
    a.download = downloadName
    document.body.appendChild(a)
    a.click()
    a.remove()
    URL.revokeObjectURL(objUrl)
  } catch (err) {
    console.error('Download failed', err)
    alert('Failed to download file')
  }
}

function toggleZoom() {
  isZoomed.value = !isZoomed.value
}
</script>
<template>
  <div class="w-full" :class="fullScreen ? 'fixed inset-0 z-50 bg-white' : 'card p-0 aspect-[210/297] max-h-[60vh] sm:max-h-[80vh]'">
    <div class="flex items-center justify-between gap-2 p-2 border-b bg-white">
      <button @click="toggleZoom" class="px-3 py-1.5 sm:py-1 bg-slate-50 hover:bg-slate-100 active:bg-slate-200 text-slate-700 rounded text-sm touch-manipulation" title="Fullscreen view">
        <svg class="w-4 h-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4" />
        </svg>
      </button>
      <button @click="downloadPreview" class="px-3 py-1.5 sm:py-1 bg-slate-50 hover:bg-slate-100 active:bg-slate-200 text-slate-700 rounded text-sm touch-manipulation">{{ $t('documents.download') }}</button>
    </div>
    <div :class="fullScreen ? 'h-[calc(100vh-3rem)]' : 'h-full overflow-auto'">
      <template v-if="isDoc">
        <WordPreview :url="previewUrl" :fullScreen="fullScreen" />
      </template>
      <template v-else-if="isText">
        <div class="w-full h-full overflow-auto bg-white p-4">
          <pre class="whitespace-pre-wrap font-mono text-sm">{{ textContent }}</pre>
        </div>
      </template>
      <template v-else>
        <div class="p-6 h-full flex flex-col items-center justify-center text-center gap-3">
          <div>Preview not available for this file type.</div>
          <a :href="previewUrl" target="_blank" class="underline">{{ $t('documents.download') }}</a>
        </div>
      </template>
    </div>
  </div>

  <!-- Fullscreen zoom modal -->
  <Teleport to="body">
    <transition name="fade">
      <div v-if="isZoomed" class="fixed inset-0 z-[9999] bg-black/80 flex items-center justify-center p-4 transition-opacity duration-300">
        <div class="bg-white rounded-lg shadow-2xl max-w-4xl w-full max-h-[90vh] flex flex-col">
          <!-- Header -->
          <div class="flex items-center justify-between gap-2 p-4 border-b">
            <h2 class="text-lg font-semibold text-slate-900">Full Preview</h2>
            <button @click="isZoomed = false" class="p-2 hover:bg-slate-100 rounded transition" title="Close">
              <svg class="w-5 h-5 text-slate-700" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <!-- Content -->
          <div class="flex-1 overflow-auto bg-white">
            <template v-if="isDoc">
              <WordPreview :url="previewUrl" :fullScreen="true" />
            </template>
            <template v-else-if="isText">
              <div class="w-full h-full overflow-auto p-6">
                <pre class="whitespace-pre-wrap font-mono text-sm">{{ textContent }}</pre>
              </div>
            </template>
            <template v-else>
              <div class="w-full h-full flex flex-col items-center justify-center text-center gap-3 p-6">
                <div>Preview not available for this file type.</div>
                <a :href="previewUrl" target="_blank" class="underline text-blue-600 hover:text-blue-800">{{ $t('documents.download') }}</a>
              </div>
            </template>
          </div>
        </div>
      </div>
    </transition>
  </Teleport>
</template>
