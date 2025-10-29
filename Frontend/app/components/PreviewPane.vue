<script setup lang="ts">
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

const previewUrl = computed(() => props.url)
const textPreviewUrl = computed(() => {
  if (props.documentId && isText.value) {
    return `${apiBase}api/documents/${props.documentId}/preview`
  }
  return props.url
})
</script>
<template>
  <div class="card p-0 w-full aspect-[210/297] max-h-[80vh] overflow-hidden">
    <template v-if="isDoc">
      <!-- Use Word preview component for exact 1:1 rendering -->
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
