<script setup lang="ts">
import type { Document } from '~/types'

const props = defineProps<{ docs: Document[], selectedId?: string | null }>()
const emit = defineEmits<{ 
  (e: 'select', id: string): void 
  (e: 'delete', id: string): void
}>()

const api = useRuntimeConfig().public.apiBase as string
function cls(id: string) { 
  return ['listbox-item', props.selectedId === id ? 'listbox-item-active' : ''].join(' ') 
}
</script>
<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="font-semibold">Uploaded Documents</div>
      <div class="muted">{{ docs.length }}</div>
    </div>
    <div class="listbox-body max-h-64 overflow-auto">
      <div v-for="d in docs" :key="d.id" :class="cls(d.id)" @click="$emit('select', d.id)">
        <div class="flex flex-col flex-1 min-w-0 mr-2">
          <div class="font-medium truncate">{{ d.fileName }}</div>
          <div class="text-xs text-slate-400">
            <div class="truncate">{{ (d.sizeBytes/1024).toFixed(1).replace('.', ',') }} KB</div>
            <div class="truncate">{{ new Date(d.uploadedAt).toLocaleDateString('et-EE') }} {{ new Date(d.uploadedAt).toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' }) }}</div>
          </div>
        </div>
          
        <div class="flex gap-1 shrink-0">
          <button @click.stop="$emit('delete', d.id)" class="px-3 py-2 text-sm rounded bg-red-50 hover:bg-red-100 text-red-600 transition font-medium">×</button>
        </div>
      </div>
    </div>
  </div>
</template>
