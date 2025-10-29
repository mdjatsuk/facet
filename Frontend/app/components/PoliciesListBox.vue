<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{ 
  policies: { id: string, name: string, options: Record<string, boolean> }[], 
  selectedId?: string | null 
}>()

const emit = defineEmits<{ 
  (e: 'select', id: string): void 
  (e: 'delete', id: string): void
  (e: 'use',id: string): void
}>()

const expandedPolicyId = ref<string | null>(null)

function cls(id: string) { 
  return ['listbox-item', props.selectedId === id ? 'listbox-item-active' : ''].join(' ') 
}

const optionLabels: Record<string, string> = {
  deleteAllEmails: 'Emails',
  removePhoneNumbers: 'Phone Numbers',
  removeNationalIds: 'ID',
  anonymizeNames: 'Names',
  removeMailingAddresses: 'Address',
  deleteIPAddresses: 'IP Address',
  removeFinancialInfo: 'Financial Info',
  stripMedicalInfo: 'Medical Info',
  removeUsernames: 'Usernames',
}

function toggleExpand(id: string) {
  expandedPolicyId.value = expandedPolicyId.value === id ? null : id
}

function getActiveOptions(policy: typeof props.policies[0]) {
  return Object.entries(policy.options)
    .filter(([_, enabled]) => enabled)
    .map(([key]) => optionLabels[key] || key)
}

function useSelected() {
  const id = props.selectedId ?? props.policies?.[0]?.id
  if (!id) return
  emit('use', id)
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

      <template v-for="p in policies" :key="p.id">
        <div :class="cls(p.id)" @click="$emit('select', p.id)" class="flex flex-col">
          <div class="flex items-center justify-between">
            <div class="flex flex-col flex-1 min-w-0 mr-2">
              <div class="font-medium truncate">{{ p.name }}</div>
            </div>
            <div class="flex gap-1 shrink-0">
              <button 
                @click.stop="toggleExpand(p.id)" 
                class="px-3 py-2 text-sm rounded bg-blue-50 hover:bg-blue-100 text-blue-600 transition font-medium"
              >
                {{ expandedPolicyId === p.id ? '▲' : '▼' }}
              </button>
              <button
                @click.stop="$emit('delete', p.id)"
                class="px-3 py-2 text-sm rounded bg-red-50 hover:bg-red-100 text-red-600 transition font-medium"
              >
                ×
              </button>
            </div>
          </div>
          
          <div v-if="expandedPolicyId === p.id" class="mt-2 text-sm pl-4 pb-2">
            <ul class="text-slate-600 list-disc pl-5">
              <li v-for="opt in getActiveOptions(p)" :key="opt">
                {{ opt }}
              </li>
              <li v-if="getActiveOptions(p).length === 0" class="text-slate-400">
                No options enabled
              </li>
            </ul>
          </div>
        </div>
      </template>
    </div>

    <div class="listbox-footer mt-3 px-3 pb-3">
      <button
        @click="useSelected"
        :disabled="policies.length === 0"
        class="w-full px-3 py-2 bg-blue-600 text-white rounded rounded-lg text-sm font-medium disabled:opacity-50"
      >
        Use Policy
      </button>
    </div>
  </div>
</template>
