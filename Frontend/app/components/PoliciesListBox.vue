<script setup lang="ts">
const props = defineProps<{ 
  policies: { id: string, name: string, options: Record<string, boolean> }[], 
  selectedId?: string | null 
}>()

const emit = defineEmits<{ 
  (e: 'select', id: string): void 
  (e: 'delete', id: string): void
  (e: 'view', id: string): void 
}>()

function cls(id: string) { 
  return ['listbox-item', props.selectedId === id ? 'listbox-item-active' : ''].join(' ') 
}
</script>

<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="font-semibold">All Policies</div>
      <div class="muted">{{ policies.length }}</div>
    </div>

    <div class="listbox-body max-h-64 overflow-auto">
      <div v-if="policies.length === 0" class="text-gray-500 p-4 text-center">
        No policies created.
      </div>

      <div v-for="p in policies" :key="p.id" :class="cls(p.id)" @click="$emit('select', p.id)">
        <div class="flex flex-col flex-1 min-w-0 mr-2">
          <div class="font-medium truncate">{{ p.name }}</div>
        </div>
        <div class="flex gap-1 shrink-0">
          <button 
            @click.stop="$emit('view', p.id)" 
            class="px-3 py-2 text-sm rounded bg-blue-50 hover:bg-blue-100 text-blue-600 transition font-medium"
          >
            👁
          </button>
          <button @click.stop="$emit('delete', p.id)" class="px-3 py-2 text-sm rounded bg-red-50 hover:bg-red-100 text-red-600 transition font-medium">×</button>
        </div>
      </div>
    </div>
  </div>
</template>
