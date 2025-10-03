<script setup lang="ts">

const props = defineProps<{ url: string, contentType?: string }>()

const isPdf = computed(() => (props.contentType || '').includes('pdf') || props.url.endsWith('.pdf'))
const isImage = computed(() => /(png|jpe?g|gif|webp|bmp|svg)/i.test(props.contentType || '') || /\.(png|jpe?g|gif|webp|bmp|svg)$/i.test(props.url))
const isText = computed(() => (props.contentType || '').startsWith('text/') || /\.(txt|csv|json|md)$/i.test(props.url))
</script>
<template>
  <div class="card p-0 h-[600px] overflow-hidden">
    <template v-if="isPdf">
      <object :data="url" type="application/pdf" class="w-full h-full">
        <iframe :src="url" class="w-full h-full"></iframe>
      </object>
    </template>
    <template v-else-if="isImage">
      <div class="w-full h-full flex items-center justify-center overflow-auto bg-white">
        <img :src="url" alt="preview" class="max-w-full max-h-full" />
      </div>
    </template>
    <template v-else-if="isText">
      <iframe :src="url" class="w-full h-full"></iframe>
    </template>
    <template v-else>
      <div class="p-6 h-full flex flex-col items-center justify-center text-center gap-3">
        <div>Preview not available for this file type.</div>
        <a :href="url" target="_blank" class="underline">Open / Download</a>
      </div>
    </template>
  </div>
</template>
