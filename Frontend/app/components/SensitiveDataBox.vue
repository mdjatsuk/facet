<template>
  <div class="listbox">
    <div class="listbox-header flex flex-wrap items-center justify-between gap-2 sm:gap-3">
        <div class="flex items-center gap-2 sm:gap-3 min-w-0">
          <svg class="w-4 h-4 sm:w-5 sm:h-5 text-primary flex-shrink-0" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 11c0-1.38-1.12-2.5-2.5-2.5S7 9.62 7 11s1.12 2.5 2.5 2.5S12 12.38 12 11zM12 3v2m0 14v2m8.66-9h-2M5.34 12H3m15.36 6.36l-1.42-1.42M7.06 7.06 5.64 5.64m12.02 0-1.42 1.42M7.06 16.94 5.64 18.36" />
          </svg>
          <div class="min-w-0">
            <div class="text-sm sm:text-base font-semibold truncate">Sensitive Data</div>
            <div class="text-xs text-slate-400 truncate">Detected types — <span class="font-medium text-slate-700">{{ allTypes.length }}</span></div>
          </div>
        </div>
        
      </div>

      <div class="listbox-body">
        <div v-if="allTypes.length === 0" class="text-gray-500 p-4 sm:p-6 text-center text-sm">
          No sensitive data found.
        </div>

        <div v-else class="px-2 sm:px-3 py-3 sm:py-4">
          <!-- Stack the type links vertically; make each link full-width and justify content so icon is left and chevron right -->
          <div class="flex flex-col gap-2">
            <NuxtLink
              v-for="t in allTypes"
              :key="t"
              :to="linkFor(t)"
              class="flex items-center justify-between w-full gap-2 px-3 py-2 sm:py-1.5 bg-slate-50 text-primary dark:bg-slate-800 dark:text-slate-200 rounded-full text-sm hover:shadow hover:bg-primary/10 hover:text-primary dark:hover:bg-slate-700 dark:hover:text-slate-100 transition-colors duration-150 focus:outline-none focus:ring-2 focus:ring-primary/30 active:scale-95 touch-manipulation"
            >
              <div class="flex items-center gap-2 min-w-0">
                <svg class="w-4 h-4 text-primary flex-shrink-0" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 11c0-1.38-1.12-2.5-2.5-2.5S7 9.62 7 11s1.12 2.5 2.5 2.5S12 12.38 12 11z" />
                </svg>
                <span class="truncate flex-1 min-w-0 capitalize">{{ t }}</span>
              </div>
              <svg class="w-3 h-3 text-slate-400 flex-shrink-0" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </NuxtLink>
          </div>
        </div>
      </div>
  </div>
</template>

<script setup lang="ts">
import { toRef, computed, ref } from 'vue'

const props = defineProps<{
  types: string[]  
  docId?: string | null
  isStaged?: boolean
  from?: string
  customTypes?: string[]
}>()

const docIdRef = toRef(props, 'docId')
const isStagedRef = toRef(props, 'isStaged')
const fromRef = toRef(props, 'from')
const customTypesRef = toRef(props, 'customTypes')

const docId = computed(() => docIdRef.value || '')
const isStaged = computed(() => isStagedRef.value ?? false)
const from = computed(() => fromRef.value)

const allTypes = computed(() => {
  const baseTypes = props.types || []
  const customs = customTypesRef.value || []
  const combined = [...new Set([...baseTypes, ...customs])]
  return combined.sort()
})

function linkFor(t: string) {
  return { path: `/sensitive/${encodeURIComponent(t)}`, query: { docId: docId.value || undefined, staged: isStaged.value ? 'true' : undefined, from: from.value || undefined } }
}
</script>