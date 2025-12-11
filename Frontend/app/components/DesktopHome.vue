<script setup lang="ts">
import type { Document } from '../types'

const props = defineProps<{
  docs: Document[]
  policies: { id: string, name: string, options: Record<string, boolean> }[]
  selectedId: string | null
  selectedPolicyId: string | null
  stagedDoc: Document | null
  uniqueTypes: string[]
  selectedCount: number
  previewUrl: string
  previewType: string
  documentId: string | undefined
  viewedPolicy: { id: string, name: string, options: Record<string, boolean> } | null
  activeOptions: string[]
  optionLabels: Record<string, string>
}>()

const emit = defineEmits<{
  (e: 'uploaded', payload: any): void
  (e: 'apply-selected'): void
  (e: 'discard-staged'): void
  (e: 'select-doc', id: string): void
  (e: 'delete-doc', id: string): void
  (e: 'select-policy', id: string): void
  (e: 'view-policy', id: string): void
  (e: 'delete-policy', id: string): void
  (e: 'use-policy'): void
  (e: 'close-viewed-policy'): void
  (e: 'pattern-found', type: string): void
}>()
</script>

<template>
  <div class="grid gap-4 sm:gap-6 lg:grid-cols-5">
    <!-- Left aside -->
    <aside class="space-y-4 lg:col-span-1 order-1">
      <UploadDrop @uploaded="(p) => emit('uploaded', p)" />
      <SensitiveDataBox :types="props.uniqueTypes" :doc-id="props.documentId" :custom-types="[]" />
      <CustomPatternSearch :doc-id="props.documentId" @pattern-found="(t) => emit('pattern-found', t)" />
      <div class="mt-2 flex flex-row gap-2 items-center">
        <button @click="emit('apply-selected')" class="px-5 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 active:bg-blue-800 touch-manipulation text-sm font-bold whitespace-nowrap" :disabled="!props.documentId || props.selectedCount === 0">
          {{ $t('sensitiveData.apply') }}
        </button>
        <div class="inline-flex items-center gap-2 px-3 py-1.5 bg-slate-100 text-slate-700 rounded-full text-sm font-medium">
          <span class="inline-flex items-center justify-center w-5 h-5 bg-blue-600 text-white rounded-full text-xs font-bold">{{ props.selectedCount }}</span>
          <span>{{ $t('sensitiveData.selected') }}</span>
        </div>
      </div>
    </aside>

    <!-- Center preview -->
    <section class="lg:col-span-3 order-2">
      <div v-if="props.stagedDoc" class="mb-3 p-3 sm:p-4 bg-white border border-slate-200 rounded-lg shadow-sm">
        <div class="flex flex-col sm:flex-row justify-between items-start gap-3 sm:gap-0">
          <div class="min-w-0 flex-1">
            <div class="text-sm font-medium text-slate-900 break-words">{{ props.stagedDoc.fileName }}</div>
            <div class="text-xs text-slate-500 mt-1">
              {{ new Date(props.stagedDoc.uploadedAt).toLocaleString() }} • {{ (props.stagedDoc.sizeBytes / 1024).toFixed(2) }} KB
            </div>
          </div>
          <button @click="emit('discard-staged')" class="px-3 py-1.5 bg-red-500 text-white text-xs rounded-md hover:bg-red-600 active:bg-red-700 transition touch-manipulation whitespace-nowrap">
            {{ $t('documents.delete') }}
          </button>
        </div>
      </div>
      <PreviewPane v-if="props.documentId" :key="props.documentId" :url="props.previewUrl" :contentType="props.previewType" :documentId="props.documentId" />
      <div v-else class="card p-6 w-full flex items-center justify-center muted" style="height: 842px; max-height: 80vh;">{{ $t('documents.nothingToDisplay') }}</div>
    </section>

    <!-- Right aside -->
    <aside class="lg:col-span-1 order-3 space-y-4">
      <DocListBox :docs="props.docs" :selected-id="props.selectedId" @select="(id: string) => emit('select-doc', id)"  @delete="(id: string) => emit('delete-doc', id)" class="mb-2"/>
      <PoliciesListBox :policies="props.policies" :selected-id="props.selectedPolicyId" @select="(id: string) => emit('select-policy', id)"  @view="(id: string) => emit('view-policy', id)" @delete="(id: string) => emit('delete-policy', id)" @use="() => emit('use-policy')" />
      <div v-if="props.viewedPolicy" class="mt-2 p-3 border rounded bg-slate-50">
        <div class="font-medium mb-1 text-sm sm:text-base">{{ props.viewedPolicy.name }}</div>
        <ul class="text-xs sm:text-sm text-slate-600 list-disc pl-5">
          <li v-for="k in props.activeOptions" :key="k">
            {{ props.optionLabels[k] || k }}
          </li>
          <li v-if="props.activeOptions.length === 0">None</li>
        </ul>
        <button
          @click="emit('close-viewed-policy')"
          class="mt-2 px-2 py-1 text-xs text-red-500 border rounded hover:bg-red-50 touch-manipulation"
        >
          Close
        </button>
      </div>
    </aside>
  </div>
</template>
