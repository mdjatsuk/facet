<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'

const { t } = useI18n()

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
  deleteAllEmails: t('policies.create.policyOptions.emails'),
  removePhoneNumbers: t('policies.create.policyOptions.phoneNumbers'),
  removeNationalIds: t('policies.create.policyOptions.nationalIds'),
  anonymizeNames: 'Names',
  removeMailingAddresses: 'Address',
  deleteIPAddresses: 'IP Address',
  removeFinancialInfo: t('policies.create.policyOptions.financialInfo'),
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
      <div class="text-sm sm:text-base font-semibold">{{ $t('policies.title') }}</div>
      <div class="muted text-xs sm:text-sm">{{ policies.length }}</div>
    </div>

    <div class="listbox-body max-h-64 overflow-auto">
      <div v-if="policies.length === 0" class="text-gray-500 p-3 sm:p-4 text-center text-sm">
        {{ $t('policies.noPolicy') }}
      </div>

      <template v-for="p in policies" :key="p.id">
        <div :class="cls(p.id)" @click="$emit('select', p.id)" class="flex flex-col">
          <div class="flex items-center justify-between">
            <div class="flex flex-col flex-1 min-w-0 mr-2">
              <div class="text-sm sm:text-base font-medium truncate">{{ p.name }}</div>
            </div>
            <div class="flex gap-1 shrink-0">
              <button 
                @click.stop="toggleExpand(p.id)" 
                class="px-3 py-2 text-base sm:text-sm rounded bg-blue-50 hover:bg-blue-100 active:bg-blue-200 text-blue-600 transition font-medium touch-manipulation"
              >
                {{ expandedPolicyId === p.id ? '▲' : '▼' }}
              </button>
              <button
                @click.stop="$emit('delete', p.id)"
                class="px-3 py-2 text-base sm:text-sm rounded bg-red-50 hover:bg-red-100 active:bg-red-200 text-red-600 transition font-medium touch-manipulation"
              >
                ×
              </button>
            </div>
          </div>
          
          <div v-if="expandedPolicyId === p.id" class="mt-2 text-xs sm:text-sm pl-3 sm:pl-4 pb-2">
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

    <div class="listbox-footer mt-2 sm:mt-3 px-2 sm:px-3 pb-2 sm:pb-3">
      <button
        @click="useSelected"
        :disabled="policies.length === 0"
        class="w-full px-3 py-2.5 sm:py-2 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white rounded-lg text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed touch-manipulation"
      >
        {{ $t('nav.usePolicy') }}
      </button>
    </div>
  </div>
</template>
