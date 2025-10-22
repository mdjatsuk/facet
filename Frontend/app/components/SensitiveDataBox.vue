<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="font-semibold">Sensitive Data</div>
      <div class="muted">{{ types.length }}</div>
    </div>

    <div class="listbox-body">
      <div v-if="types.length === 0" class="text-gray-500 p-4 text-center">
        No sensitive data found.
      </div>

      <ul v-else class="list-disc list-inside px-4 py-3 space-y-1 text-sm text-slate-700">
        <li v-for="t in types" :key="t">
            <NuxtLink :to="linkFor(t)" class="underline hover:text-blue-600">
              {{ t }}
            </NuxtLink>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
import { toRef, computed } from 'vue'

const props = defineProps<{
  types: string[]  
  docId?: string | null
}>()

const docIdRef = toRef(props, 'docId')
const docId = computed(() => docIdRef.value || '')

import { useSelection } from '~/composables/useSelection'
import { useApi } from '~/composables/useApi'
const { toggle, isSelected, getValues, count } = useSelection()
const { postBlob } = useApi()


function linkFor(t: string) {
  return { path: `/sensitive/${encodeURIComponent(t)}`, query: { docId: docId.value || undefined } }
}
</script>