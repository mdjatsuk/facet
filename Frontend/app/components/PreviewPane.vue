<script setup lang="ts">
import { computed, ref, watch, onMounted, onBeforeUnmount } from 'vue'
const props = defineProps<{ url: string, contentType?: string, documentId?: string }>()

const config = useRuntimeConfig()
const apiBase = config.public.apiBase as string

const isDoc = computed(() =>
  /(msword|wordprocessingml)/i.test(props.contentType || '') ||
  /\.(doc|docx)$/i.test(props.url)
)
const isText = computed(() =>
  (props.contentType || '').startsWith('text/') ||
  /\.(txt)$/i.test(props.url)
)


const objectUrl = ref<string | null>(null)
const tokenState = useState<string | undefined>('token')

async function loadObjectUrl() {
  if (objectUrl.value) {
    URL.revokeObjectURL(objectUrl.value)
    objectUrl.value = null
  }

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
  } catch (e) {
    console.error('Failed to fetch preview with token', e)
  }
}

watch(() => [props.url, props.documentId, tokenState.value], () => {
  loadObjectUrl()
})

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
        const metaResp = await fetch(`${apiBase}api/documents/${props.documentId}`)
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
</script>
<template>
  <div class="card p-0 w-full aspect-[210/297] max-h-[80vh] overflow-hidden">
    <div class="flex items-center justify-end gap-2 p-2 border-b bg-white">
      <button @click="downloadPreview" class="px-3 py-1 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded">Download</button>
    </div>
    <template v-if="isDoc">
      <WordPreview :url="previewUrl" />
    </template>
    <template v-else-if="isText">
      <iframe :src="textPreviewUrl" class="w-full h-full border-0"></iframe>
    </template>
    <template v-else>
      <div class="p-6 h-full flex flex-col items-center justify-center text-center gap-3">
        <div>Preview not available for this file type.</div>
        <a :href="previewUrl" target="_blank" class="underline">Open / Download</a>
      </div>
    </template>
  </div>
</template>
