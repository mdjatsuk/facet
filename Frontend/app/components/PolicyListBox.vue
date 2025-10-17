<script setup lang="ts">
const props = defineProps<{ policies: Array<{ id: string, name: string, options: Record<string, boolean>, createdAt: string }> }>()

const emit = defineEmits<{ delete: [id: string] }>()

function getEnabledOptions(policy: typeof props.policies[number]) {
  const labels: Record<string, string> = {
    deleteAllEmails: 'Email addresses',
    removePhoneNumbers: 'Phone numbers',
    removeNationalIds: 'National IDs',
    anonymizeNames: 'Personal names',
    removeMailingAddresses: 'Mailing addresses',
    deleteIPAddresses: 'IP addresses',
    removeFinancialInfo: 'Financial info',
    stripMedicalInfo: 'Medical info',
    removeUsernames: 'Usernames'
  }
  return Object.entries(policy.options)
    .filter(([_, value]) => value)
    .map(([key]) => labels[key] || key)
}
</script>
<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="font-semibold">Created Policies</div>
      <div class="muted">{{ policies.length }}</div>
    </div>
    <div class="listbox-body max-h-96 overflow-auto">
      <div v-if="policies.length === 0" class="text-gray-500 p-4 text-center">
        No policies created yet.
      </div>
      <div v-for="p in policies" :key="p.id" class="border-b border-slate-100 p-3 hover:bg-slate-50 transition">
        <div class="flex items-start justify-between gap-2">
          <div class="flex-1 min-w-0">
            <div class="font-medium text-slate-800 truncate mb-1">{{ p.name }}</div>
            <div class="text-xs text-slate-500 mb-2">
              {{ new Date(p.createdAt).toLocaleDateString('et-EE') }} {{ new Date(p.createdAt).toLocaleTimeString('et-EE', { hour: '2-digit', minute: '2-digit' }) }}
            </div>
            <div class="flex flex-wrap gap-1">
              <span v-for="opt in getEnabledOptions(p)" :key="opt" class="text-xs bg-blue-50 text-blue-700 px-2 py-1 rounded">
                {{ opt }}
              </span>
            </div>
          </div>
          <button @click="emit('delete', p.id)" class="px-2 py-1 text-sm rounded bg-red-50 hover:bg-red-100 text-red-600 transition font-medium shrink-0">×</button>
        </div>
      </div>
    </div>
  </div>
</template>
