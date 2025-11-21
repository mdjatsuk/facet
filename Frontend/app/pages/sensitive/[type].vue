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
const isStaged = computed(() => (route.query.staged as string) === 'true')

const allDetected = computed(() => {
  const data = getDetected(docId.value || '')
  return data
})

const itemsForType = computed(() => allDetected.value.filter(d => d.type === type.value))

const { toggle, isSelected, toggleAll, getValues } = useSelection()

const selectedValues = computed(() => getValues(docId.value))

const allSelected = computed(() => {
  const items = itemsForType.value || []
  if (items.length === 0) return false
  return items.every(i => selectedValues.value.includes(i.value))
})

function toggleItemByValue(value: string) {
  if (!value) return
  toggle(docId.value, value, value)
}

function selectAll() {
  const values = itemsForType.value.map(i => i.value)
  toggleAll(docId.value, values)
}

onMounted(() => {
  if (!docId.value) router.push('/')
})

function applyAndBack() {
  // Return to home page on PC, or sensitive-data page on mobile
  // Check if coming from sensitive-data page (mobile workflow)
  const referrer = route.query.from as string
  if (referrer === 'sensitive-data') {
    // Mobile workflow: return to sensitive-data page
    router.push({ path: '/sensitive-data', query: { docId: docId.value, staged: isStaged.value ? 'true' : undefined } })
  } else {
    // PC workflow: just return to home page without query params
    router.push({ path: '/' })
  }
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
        <li v-for="(item, idx) in itemsForType" :key="item.value + '::' + idx" class="flex items-center gap-3 p-3 border rounded">
          <input 
            type="checkbox" 
            :checked="isSelected(docId, item.value)" 
            @change="() => toggleItemByValue(item.value)" 
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
          Confirm Selection
        </button>
        <NuxtLink to="/" class="px-4 py-2 bg-transparent hover:bg-slate-50 text-slate-700 rounded-lg">Back</NuxtLink>
      </div>
    </div>
  </div>
</template>

