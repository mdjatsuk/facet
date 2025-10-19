<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'  
import { useRoute, useRouter } from 'vue-router'  
import { useDetected } from '~/composables/useDetected' 

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
const selectedItems = ref<Set<number>>(new Set())  

function toggleItem(index: number) {
  if (selectedItems.value.has(index)) {
    selectedItems.value.delete(index)
  } else {
    selectedItems.value.add(index)
  }
}

function hideAll() {
  itemsForType.value.forEach((_, idx) => selectedItems.value.add(idx))
  console.log('Hide all selected:', Array.from(selectedItems.value))
}

function applyHide() {
  if (selectedItems.value.size === 0) return
  alert(`Hiding ${selectedItems.value.size} items of type ${type.value} in doc ${docId.value}`)
   router.push('/')
}

onMounted(() => {
  if (!docId.value) {
    console.error('No docId in query! Redirecting to index.')
    router.push('/')
  }
})
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
            :checked="selectedItems.has(idx)" 
            @change="toggleItem(idx)" 
            class="accent-blue-500 h-4 w-4"
          />
          <span class="font-mono text-sm break-all">{{ item.value }}</span>

          <span class="text-xs text-slate-400 ml-2">({{ item.indexStart }}–{{ item.indexEnd }})</span>
        </li>
      </ul>

      <div class="flex gap-3 justify-end">
        <button 
          @click="hideAll" 
          class="px-4 py-2 bg-red-500 hover:bg-red-600 text-white rounded-lg"
          :disabled="itemsForType.length === 0"
        >
          Hide all
        </button>
        <button 
          @click="applyHide" 
          :disabled="selectedItems.size === 0"
          class="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg disabled:opacity-50"
        >
          Apply Hide ({{ selectedItems.size }} selected)
        </button>
        <NuxtLink to="/" class="px-4 py-2 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded-lg">Back</NuxtLink>
      </div>
    </div>
  </div>
</template>