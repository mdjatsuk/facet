<script setup lang="ts">
import { ref } from 'vue'
import { useApi } from '../composables/useApi'
import { useAuth } from '~/composables/useAuth'
import type { UploadResult, Document, ApiError } from '~/types'

const emit = defineEmits<{ (e: 'uploaded', payload: { document: Document, detected?: import('~/types').SensitiveItem[] }): void }>()

const { upload } = useApi()
const auth = useAuth()
const dragOver = ref(false)
const busy = ref(false)
const toast = ref<{ type: 'success' | 'error' | 'info', message: string } | null>(null)
const MAX_UPLOAD_BYTES = 10 * 1024 * 1024
const allowedExt = ['.doc', '.docx', '.txt']
const fileInput = ref<HTMLInputElement | null>(null)

function openFilePicker() {
  fileInput.value?.click()
}

function onZoneClick() {
  if (dragOver.value || busy.value) return
  openFilePicker()
}

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
  if (!allowedExt.includes(ext)) return showToast('error', 'Unsupported file type. Allowed: DOC, DOCX, TXT.')
  if (file.size > MAX_UPLOAD_BYTES) return showToast('error', 'File is too large. Maximum 10 MB allowed.')
  busy.value = true
  try {
    const form = new FormData()
    form.append('file', file)
    const res = await auth.fetchWithToken<UploadResult>('documents/upload', { method: 'POST', body: form })
    if (res.success && res.document) {
      emit('uploaded', { document: res.document, detected: (res as any).detected })
    } else {
      showToast('error', res.message || 'Upload failed')
    }
  } catch (e: unknown) {
    const error = e as ApiError
    showToast('error', error?.data || error?.message || 'Upload failed')
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
  <div class="flex justify-center">
    <div class="w-56">
      <div
        class="group w-full h-28 bg-white rounded-2xl border-2 border-dashed flex items-center justify-center px-4 hover:shadow-soft hover:bg-primary/10 hover:border-primary transition transform duration-150 hover:scale-105 cursor-pointer focus:outline-none focus:ring-2 focus:ring-primary/30 focus:ring-offset-1"
        :class="[dragOver ? 'border-primary bg-primary/12' : 'border-slate-200']"
        @click="onZoneClick"
        @dragover.prevent="dragOver = true"
        @dragenter.prevent="dragOver = true"
        @dragleave.prevent="dragOver = false"
        @drop.prevent="onDrop"
        tabindex="0"
        @keydown.enter.prevent="openFilePicker"
        @keydown.space.prevent="openFilePicker"
        role="region"
        aria-label="Upload document"
      >
        <div class="flex flex-col items-center gap-1 text-center">
          <svg class="h-6 w-6 text-primary" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M4 16v1a2 2 0 002 2h12a2 2 0 002-2v-1M12 12V4m0 0l3.5 3.5M12 4L8.5 7.5" />
          </svg>
          <div class="text-lg font-semibold text-slate-800">Upload file</div>
          <p class="text-xs text-slate-500">DOC, DOCX, TXT — up to 10MB</p>
          <input ref="fileInput" type="file" accept=".doc,.docx,.txt" @change="onInput" class="hidden" id="fileInput">
          <div v-if="busy" class="text-xs text-slate-500 animate-pulse mt-1">Uploading…</div>
        </div>
      </div>

      <div class="mt-2 flex justify-center">
        <UiToast v-if="toast" :type="toast.type" :message="toast.message"/>
      </div>
    </div>
  </div>
</template>
