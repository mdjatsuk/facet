<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'  
import { useRoute, useRouter } from 'vue-router'  
import { useDetected } from '~/composables/useDetected' 
import { useSelection } from '~/composables/useSelection'

const route = useRoute()
const router = useRouter()

const type = ref((route.params.type as string) || '')  

const { getDetected } = useDetected()

const docId = computed(() => (route.query.docId as string) || '')

const allDetected = computed(() => {
  const data = getDetected(docId.value || '')
  return data
})

const itemsForType = computed(() => allDetected.value.filter(d => d.type === type.value))

const { toggle, isSelected } = useSelection()

function toggleItem(index: number) {
  const items = itemsForType.value || []
  const item = items[index]
  if (!item) return
  const key = `${type.value}:${index}`
  toggle(docId.value, key, item.value)
  try {
    console.log('toggled', { docId: docId.value, key, value: item.value, count: (typeof window !== 'undefined' ? (window.localStorage.getItem('selectedToHide') || '') : '') })
  } catch (e) { }
}

function selectAll() {
  const items = itemsForType.value || []
  items.forEach((item, idx) => {
    const key = `${type.value}:${idx}`

    if (!isSelected(docId.value, key)) toggle(docId.value, key, item.value)
  })
  try { console.log('selectAll', { docId: docId.value, total: itemsForType.value.length }) } catch {}
}

onMounted(() => {
  if (!docId.value) {
    console.error('No docId in query! Redirecting to index.')
    router.push('/')
  }
})

function applyAndBack() {
  router.push('/')
}
</script>

<template>
  <div class="max-w-3xl mx-auto py-8">
    <div class="card p-6">
      <h1 class="text-2xl font-semibold mb-4">Sensitive {{ type }} Data</h1>
      
      <div v-if="docId === ''" class="text-red-500 p-4 text-center">
        Error: No document selected. Go back and select a file.
      </div>
      <div v-else-if="itemsForType.length === 0" class="text-gray-500 p-4 text-center">
        No {{ type }} data found in this document.
      </div>

      <ul v-else class="space-y-2 mb-4">
        <li v-for="(item, idx) in itemsForType" :key="idx" class="flex items-center gap-3 p-3 border rounded">
          <input 
            type="checkbox" 
            :checked="isSelected(docId, `${type}:${idx}`)" 
            @change="toggleItem(idx)" 
            class="accent-blue-500 h-4 w-4"
          />
          <span class="font-mono text-sm break-all">{{ item.value }}</span>

          <span class="text-xs text-slate-400 ml-2">({{ item.indexStart }}–{{ item.indexEnd }})</span>
        </li>
      </ul>

      <div class="flex gap-3 justify-end">
          <button
            @click="selectAll"
            class="px-4 py-2 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded-lg"
          >
            Select all
          </button>
        <button
          @click="applyAndBack"
          class="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg disabled:opacity-50"
          :disabled="itemsForType.length === 0"
        >
          Apply selection
        </button>
        <NuxtLink to="/" class="px-4 py-2 bg-transparent hover:bg-slate-50 text-slate-700 rounded-lg">Back</NuxtLink>
      </div>
    </div>
  </div>
</template>

