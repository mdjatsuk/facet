<script setup lang="ts">
import type { Document, RiskInfo } from '../types'

const props = defineProps<{ 
  stagedDoc: Document | null,
  selectedDoc: Document | null,
  uniqueTypes: string[],
  riskByDoc?: Record<string, RiskInfo>
}>()

const emit = defineEmits<{
  (e: 'uploaded', payload: any): void
  (e: 'discard-staged'): void
}>()

function riskFor(id?: string | null) {
  if (!id) return null
  return props.riskByDoc?.[id] || null
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
  <!-- Upload area -->
  <UploadDrop @uploaded="(p) => emit('uploaded', p)" />

  <!-- Preview Card: staged or selected document -->
  <div class="space-y-4">
    <!-- Staged Document -->
    <div v-if="props.stagedDoc" class="bg-white border border-slate-200 rounded-lg shadow-sm p-4">
      <div class="flex items-start gap-3 mb-3">
        <div class="w-12 h-12 bg-gradient-to-br from-blue-500 to-blue-600 rounded-lg flex items-center justify-center flex-shrink-0">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-sm font-semibold text-slate-900 break-words mb-1 flex items-center gap-2">
            <span class="truncate">{{ props.stagedDoc.fileName }}</span>
            <span v-if="riskFor(props.stagedDoc.id)" :class="['inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[11px] font-semibold uppercase tracking-wide', riskClass(riskFor(props.stagedDoc.id)?.level as RiskInfo['level'])]">
              <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.29 3.86l-7.6 13.21A1 1 0 003.42 19h17.16a1 1 0 00.86-1.5L13.84 3.86a1 1 0 00-1.72 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v4m0 4h.01" />
              </svg>
              <span class="whitespace-nowrap">{{ $t(`documents.risk.${riskFor(props.stagedDoc.id)?.level}`) }}</span>
            </span>
          </div>
          <div class="text-xs text-slate-500">
            {{ new Date(props.stagedDoc.uploadedAt).toLocaleString() }}
          </div>
          <div class="text-xs text-slate-500">
            {{ (props.stagedDoc.sizeBytes / 1024).toFixed(2) }} KB
          </div>
        </div>
      </div>
      <div class="flex gap-2">
        <NuxtLink 
          :to="{ path: '/preview', query: { docId: props.stagedDoc.id, staged: 'true' } }"
          class="flex-1 px-4 py-2.5 bg-primary text-white rounded-lg hover:bg-primary/90 active:bg-primary/80 text-center text-sm font-medium touch-manipulation transition"
        >
          {{ $t('preview.viewEdit') || 'View & Edit' }}
        </NuxtLink>
        <button 
          @click="emit('discard-staged')" 
          class="px-4 py-2.5 bg-red-50 text-red-600 rounded-lg hover:bg-red-100 active:bg-red-200 text-sm font-medium touch-manipulation transition"
        >
          {{ $t('documents.delete') }}
        </button>
      </div>
    </div>

    <!-- Selected Document -->
    <div v-else-if="props.selectedDoc" class="bg-white border border-slate-200 rounded-lg shadow-sm p-4">
      <div class="flex items-start gap-3 mb-3">
        <div class="w-12 h-12 bg-gradient-to-br from-slate-600 to-slate-700 rounded-lg flex items-center justify-center flex-shrink-0">
          <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        </div>
        <div class="flex-1 min-w-0">
          <div class="text-sm font-semibold text-slate-900 break-words mb-1 flex items-center gap-2">
            <span class="truncate">{{ props.selectedDoc.fileName }}</span>
            <span v-if="riskFor(props.selectedDoc.id)" :class="['inline-flex items-center gap-1.5 px-2 py-0.5 rounded-full text-[11px] font-semibold uppercase tracking-wide', riskClass(riskFor(props.selectedDoc.id)?.level as RiskInfo['level'])]">
              <svg class="w-3.5 h-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                <path stroke-linecap="round" stroke-linejoin="round" d="M10.29 3.86l-7.6 13.21A1 1 0 003.42 19h17.16a1 1 0 00.86-1.5L13.84 3.86a1 1 0 00-1.72 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v4m0 4h.01" />
              </svg>
              <span class="whitespace-nowrap">{{ $t(`documents.risk.${riskFor(props.selectedDoc.id)?.level}`) }}</span>
            </span>
          </div>
          <div class="text-xs text-slate-500">
            {{ new Date(props.selectedDoc.uploadedAt).toLocaleString() }}
          </div>
          <div class="text-xs text-slate-500">
            {{ (props.selectedDoc.sizeBytes / 1024).toFixed(2) }} KB
          </div>
        </div>
      </div>
      <NuxtLink 
        :to="{ path: '/preview', query: { docId: props.selectedDoc.id } }"
        class="block w-full px-4 py-2.5 bg-primary text-white rounded-lg hover:bg-primary/90 active:bg-primary/80 text-center text-sm font-medium touch-manipulation transition"
      >
        {{ $t('preview.viewEdit') || 'View & Edit' }}
      </NuxtLink>
    </div>

    <!-- No Document Selected -->
    <div v-else class="bg-white border-2 border-dashed border-slate-200 rounded-lg p-8 text-center">
      <svg class="w-16 h-16 mx-auto text-slate-300 mb-3" fill="none" stroke="currentColor" stroke-width="1.5" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
      </svg>
      <p class="text-slate-500 text-sm mb-1">{{ $t('documents.noSelected') || 'No document selected' }}</p>
      <p class="text-slate-400 text-xs">{{ $t('documents.uploadOrSelect') || 'Upload a file or select from menu' }}</p>
    </div>
  </div>

  <!-- Sensitive Data Quick Info -->
  <div v-if="props.uniqueTypes.length > 0" class="bg-amber-50 border border-amber-200 rounded-lg p-4">
    <div class="flex items-center gap-2 mb-2">
      <svg class="w-5 h-5 text-amber-600" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
      </svg>
      <span class="text-sm font-semibold text-amber-900">{{ $t('sensitiveData.typesFound', { count: props.uniqueTypes.length }) || (props.uniqueTypes.length + ' sensitive data type(s) found') }}</span>
    </div>
    <div class="flex flex-wrap gap-2">
      <span v-for="t in props.uniqueTypes" :key="t" class="px-2 py-1 bg-amber-100 text-amber-800 rounded text-xs font-medium capitalize">
        {{ t }}
      </span>
    </div>
  </div>
</template>
