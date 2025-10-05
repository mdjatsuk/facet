<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '../composables/useApi'
import type { UploadResult, Document, ApiError } from '~/types'

const emit = defineEmits<{ (e: 'uploaded', doc: Document): void }>()

const { upload } = useApi()
const dragOver = ref(false)
const busy = ref(false)
const toast = ref<{ type: 'success' | 'error' | 'info', message: string } | null>(null)
const MAX_UPLOAD_BYTES = 10 * 1024 * 1024
const allowedExt = ['.pdf', '.jpg', '.jpeg', '.png']

function extOf(name: string) { 
  const i = name.lastIndexOf('.') 
  return i >= 0 ? name.slice(i).toLowerCase() : '' 
}

function showToast(type: 'success' | 'error' | 'info', message: string) { 
  toast.value = { type, message }
  setTimeout(() => toast.value = null, 3500) 
}

async function doUpload(file: File) {
  const ext = extOf(file.name)
  if (!allowedExt.includes(ext)) return showToast('error', 'Pole toetatud failitüüp. Lubatud: PDF, JPG, PNG.')
  if (file.size > MAX_UPLOAD_BYTES) return showToast('error', 'Fail on liiga suur. Lubatud kuni 10 MB.')
  busy.value = true
  try {
    const res = await upload<UploadResult>('api/documents/upload', file)
    if (res.success && res.document) {
      showToast('success', 'Fail on edukalt üles laetud')
      emit('uploaded', res.document)
    } else {
      showToast('error', res.message || 'Üleslaadimine ebaõnnestus')
    }
  } catch (e: unknown) {
    const error = e as ApiError
    showToast('error', error?.data || error?.message || 'Üleslaadimine ebaõnnestus')
  } finally { 
    busy.value = false 
  }
}

function onInput(e: Event) { 
  const input = e.target as HTMLInputElement
  const f = input.files?.[0]
  if (f) doUpload(f)
  input.value = '' 
}

function onDrop(e: DragEvent) { 
  dragOver.value = false
  const f = e.dataTransfer?.files?.[0]
  if (f) doUpload(f) 
}
</script>
<template>
  <div class="space-y-3">
    <div class="card p-6 border-dashed" :class="[dragOver ? 'border-primary bg-sky-50' : 'border-slate-200']"
         @dragover.prevent="dragOver=true" @dragleave.prevent="dragOver=false" @drop.prevent="onDrop">
      <div class="flex flex-col items-center gap-2 text-center">
        <div class="text-lg font-semibold">Lohista fail siia või vali</div>
        <p class="muted">Lubatud: PDF, JPG, PNG • kuni 10MB</p>
        <input type="file" accept=".pdf,.jpg,.jpeg,.png" @change="onInput" class="hidden" id="fileInput">
        <label for="fileInput" class="btn btn-primary mt-2 cursor-pointer">Vali fail</label>
        <div v-if="busy" class="mt-2 text-sm text-slate-500 animate-pulse">Laadin üles…</div>
      </div>
    </div>
    <UiToast v-if="toast" :type="toast.type" :message="toast.message"/>
  </div>
</template>
