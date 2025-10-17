<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../../composables/useApi'
import { useRuntimeConfig } from 'nuxt/app'

const router = useRouter()
// keep useApi import for parity if needed later
const { get, del } = useApi()
const apiBase = useRuntimeConfig().public.apiBase as string

const name = ref('')
const options = ref<Record<string, boolean>>({
  deleteAllEmails: false,
  removePhoneNumbers: false,
  removeNationalIds: false,
  anonymizeNames: false,
  removeMailingAddresses: false,
  deleteIPAddresses: false,
  removeFinancialInfo: false,
  stripMedicalInfo: false,
  removeUsernames: false,
})

const saving = ref(false)
const message = ref<string | null>(null)

const optionList = [
  { key: 'deleteAllEmails', label: 'Delete all email addresses', desc: 'e.g., example@gmail.com, john.doe@company.com' },
  { key: 'removePhoneNumbers', label: 'Remove phone numbers', desc: 'e.g., mobile, landline, or international formats' },
  { key: 'removeNationalIds', label: 'Remove national ID numbers', desc: 'e.g., SSN, passport numbers, driver’s license numbers' },
  { key: 'anonymizeNames', label: 'Anonymize personal names', desc: 'e.g., replacing "John Smith" with "Person A" or "[REDACTED]"' },
  { key: 'removeMailingAddresses', label: 'Remove mailing addresses', desc: 'e.g., street names, apartment numbers, ZIP codes' },
  { key: 'deleteIPAddresses', label: 'Delete IP addresses', desc: 'e.g., IPv4 or IPv6 addresses' },
  { key: 'removeFinancialInfo', label: 'Remove financial information', desc: 'e.g., credit card numbers, bank account numbers' },
  { key: 'stripMedicalInfo', label: 'Strip medical or health information', desc: 'e.g., diagnoses, treatment history' },
  { key: 'removeUsernames', label: 'Remove usernames or login credentials', desc: 'e.g., "admin123", "user@example.com"' }
]

async function submit() {
  if (!name.value.trim()) {
    message.value = 'Please enter a policy name'
    return
  }
  saving.value = true
  message.value = null
  try {
    const newPolicy = {
      id: crypto.randomUUID(),
      name: name.value,
      options: { ...options.value },
      createdAt: new Date().toISOString()
    }
    const policies = JSON.parse(localStorage.getItem('policies') || '[]')
    policies.push(newPolicy)
    localStorage.setItem('policies', JSON.stringify(policies))
    await router.push({ path: '/', query: { notice: 'policy-created' } })
  } catch (err: any) {
    message.value = err?.message || String(err)
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div class="max-w-3xl mx-auto py-8">
    <div class="card p-6">
      <h1 class="text-2xl font-semibold mb-4">Create new policy</h1>

      <label class="block mb-3">
        <span class="text-sm text-slate-600">Policy name</span>
        <input v-model="name" class="mt-1 block w-full rounded-md border px-3 py-2" placeholder="e.g. Remove confidential info" />
      </label>

      <div class="mb-4">
        <h2 class="font-medium mb-2">Options</h2>
        <div class="space-y-3">
          <label v-for="opt in optionList" :key="opt.key" class="flex items-start gap-3">
            <input type="checkbox" v-model="options[opt.key]" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">{{ opt.label }}</div>
              <div class="text-sm muted">{{ opt.desc }}</div>
            </div>
          </label>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <button @click="submit" :disabled="saving" class="px-4 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg">{{ saving ? 'Saving...' : 'Create policy' }}</button>
        <NuxtLink to="/" class="px-4 py-2 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded-lg">Back</NuxtLink>
      </div>

      <p v-if="message" class="mt-4 muted">{{ message }}</p>
    </div>
  </div>
</template>
