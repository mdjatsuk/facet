<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'  
import { useRoute, useRouter } from 'vue-router'  
import { useI18n } from 'vue-i18n'
import { useDetected } from '~/composables/useDetected' 
import { useSelection } from '~/composables/useSelection'

const route = useRoute()
const router = useRouter()
const { t } = useI18n()

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

const fromPage = computed(() => (route.query.from as string) || 'home')

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
  // Clear any old sessionStorage data first to avoid conflicts
  sessionStorage.removeItem('detectedItemsOnReturn')
  
  // Store the current docId and detected items so they persist on return
  if (docId.value) {
    // Also store the detected items in case they were modified by custom pattern search
    const detected = getDetected(docId.value)
    if (detected && detected.length > 0) {
      sessionStorage.setItem('detectedItemsOnReturn', JSON.stringify({
        docId: docId.value,
        items: detected,
        timestamp: Date.now()  // Add timestamp to prevent stale data issues
      }))
    }
  }
  
  // Navigate back to sensitive-data page if that's where we came from (mobile)
  if (fromPage.value === 'sensitive-data') {
    router.push({ path: '/sensitive-data', query: { docId: docId.value, staged: isStaged.value ? 'true' : undefined } })
  } else {
    // Default: navigate to home with docId (desktop)
    router.push({ path: '/', query: { docId: docId.value, fromSensitive: 'true' } })
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto py-8">
    <div class="card p-6">
      <h1 class="text-2xl font-semibold mb-4">{{ t('sensitiveData.dataTitle', { type: type }) }}</h1>
      
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

          <span v-if="item.indexStart >= 0" class="text-xs text-slate-400 ml-2">({{ item.indexStart }}–{{ item.indexEnd }})</span>
          <span v-else class="text-xs text-slate-400 ml-2">{{ $t('sensitiveData.customMatch') }}</span>
  </li>
      </ul>

      <div class="flex gap-3 justify-end">
          <button
            @click="selectAll"
            class="px-4 py-2 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded-lg"
          >
            {{ $t('sensitiveData.selectAll') }}
          </button>
        <button
          @click="applyAndBack"
          class="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg disabled:opacity-50"
          :disabled="itemsForType.length === 0"
        >
          {{ $t('sensitiveData.confirmSelection') }}
        </button>
        <NuxtLink to="/" class="px-4 py-2 bg-transparent hover:bg-slate-50 text-slate-700 rounded-lg">{{ $t('sensitiveData.back') }}</NuxtLink>
      </div>
    </div>
  </div>
</template>

