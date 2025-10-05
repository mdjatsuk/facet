<script setup lang="ts">
import type { Document } from '~/types'

const props = defineProps<{ docs: Document[], selectedId?: string | null }>()
const emit = defineEmits<{ 
  (e: 'select', id: string): void 
  (e: 'delete', id: string): void
  (e: 'highlight', id: string): void
}>()

const api = useRuntimeConfig().public.apiBase as string
function cls(id: string) { 
  return ['listbox-item', props.selectedId === id ? 'listbox-item-active' : ''].join(' ') 
}
</script>
<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="font-semibold">Laaditud dokumendid</div>
      <div class="muted">{{ docs.length }}</div>
    </div>
    <div class="listbox-body">
      <div v-for="d in docs" :key="d.id" :class="cls(d.id)" @click="$emit('highlight', d.id)">
        <div class="flex flex-col min-w-0">
          <div class="font-medium truncate max-w-[220px]">{{ d.fileName }}</div>
          <div class="muted">{{ (d.sizeBytes/1024).toFixed(1) }} KB · {{ new Date(d.uploadedAt).toLocaleString() }}</div>
        </div>
          
        <div class="flex gap-2 shrink-0">
          <button @click.stop="$emit('select', d.id)" class="btn btn-ghost ml-3 shrink-0">Ava</button>
          <button @click.stop="$emit('delete', d.id)" class="btn btn-ghost text-red-500">❌</button>
        </div>
      </div>
    </div>
  </div>
</template>
