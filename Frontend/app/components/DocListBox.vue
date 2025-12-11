<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { Document, RiskInfo } from '~/types'

const { t } = useI18n()

const props = defineProps<{ docs: Document[], selectedId?: string | null, riskById?: Record<string, RiskInfo> }>()
const emit = defineEmits<{ 
  (e: 'select', id: string): void 
  (e: 'delete', id: string): void
}>()

const api = useRuntimeConfig().public.apiBase as string

// Ensure each document appears only once in the list (by id)
const uniqueDocs = computed(() => {
  const seen = new Set<string>()
  const result: Document[] = []
  for (const d of props.docs) {
    if (!d?.id || seen.has(d.id)) continue
    seen.add(d.id)
    result.push(d)
  }
  return result
})

function cls(id: string) { 
  return ['listbox-item', props.selectedId === id ? 'listbox-item-active' : ''].join(' ') 
}

function riskFor(id: string) {
  return props.riskById?.[id]
}

function riskClass(level: RiskInfo['level']) {
  if (level === 'high') return 'bg-red-100 text-red-700 border border-red-200'
  if (level === 'medium') return 'bg-orange-100 text-orange-700 border border-orange-200'
  return 'bg-emerald-100 text-emerald-700 border border-emerald-200'
}

function dotClass(level: RiskInfo['level']) {
  if (level === 'high') return 'bg-red-500'
  if (level === 'medium') return 'bg-orange-500'
  return 'bg-emerald-500'
}
</script>
<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="text-sm sm:text-base font-semibold">{{ $t('documents.redactedTitle') }}</div>
      <div class="muted text-xs sm:text-sm">{{ docs.length }}</div>
    </div>

    <div class="listbox-body max-h-64 overflow-auto">
    <div v-if="docs.length === 0" class="text-gray-500 p-3 sm:p-4 text-center text-sm">
             {{ $t('documents.redactedNoDocuments') }}
    </div>

      <div v-for="d in uniqueDocs" :key="d.id" :class="cls(d.id)" @click="$emit('select', d.id)">
        <div class="flex flex-col flex-1 min-w-0 mr-2">
          <div class="text-sm sm:text-base font-medium min-w-0">
            <span class="truncate block">{{ d.fileName }}</span>
          </div>
          <div class="text-xs text-slate-400 flex items-center gap-2 flex-wrap">
            <span class="shrink-0">{{ (d.sizeBytes/1024).toFixed(1).replace('.', ',') }} KB</span>
            <span class="shrink-0">{{ new Date(d.uploadedAt).toLocaleDateString('et-EE') }} {{ new Date(d.uploadedAt).toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' }) }}</span>
            <span v-if="riskFor(d.id)" :class="['hidden sm:inline-flex items-center gap-1 px-1.5 py-0.5 rounded-full text-[10px] font-semibold uppercase tracking-wide shrink-0', riskClass(riskFor(d.id)?.level as RiskInfo['level'])]">
              <svg class="w-2.5 h-2.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.29 3.86l-7.6 13.21A1 1 0 003.42 19h17.16a1 1 0 00.86-1.5L13.84 3.86a1 1 0 00-1.72 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v4m0 4h.01" />
              </svg>
              <span class="whitespace-nowrap">{{ $t(`documents.risk.${riskFor(d.id)?.level}`) }}</span>
            </span>
          </div>
        </div>
        <div class="flex gap-1 shrink-0 ml-2">
          <button @click.stop="$emit('delete', d.id)" class="px-3 py-2 text-base sm:text-sm rounded bg-red-50 hover:bg-red-100 active:bg-red-200 text-red-600 transition font-medium touch-manipulation">×</button>
        </div>
      </div>
    </div>
  </div>
</template>
