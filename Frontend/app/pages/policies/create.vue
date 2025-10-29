
<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useApi } from '../../composables/useApi'
import { useRuntimeConfig } from 'nuxt/app'

const router = useRouter()
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

async function submit() {
  if (!name.value.trim()) {
    message.value = 'Please enter a Policy Name before creating.'
    return
  }
  saving.value = true
  message.value = null
  try {
    const newPolicy = {
      id: Date.now().toString(),
      name: name.value,
      options: { ...options.value }
    }

    const saved = JSON.parse(localStorage.getItem('policies') || '[]')

    saved.push(newPolicy)

    localStorage.setItem('policies', JSON.stringify(saved))

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
          <label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.deleteAllEmails" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Emails</div>
              <div class="text-sm muted">e.g., example@gmail.com, john.doe@company.com</div>
            </div>
          </label>

          <label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.removePhoneNumbers" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Phone numbers</div>
              <div class="text-sm muted">e.g., mobile, landline, or international formats</div>
            </div>
          </label>

          <label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.removeNationalIds" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">National ID numbers</div>
              <div class="text-sm muted">e.g., SSN, passport numbers, driver’s license numbers</div>
            </div>
          </label>

          <!--<label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.anonymizeNames" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Anonymize personal names</div>
              <div class="text-sm muted">e.g., replacing "John Smith" with "Person A" or "[REDACTED]"</div>
            </div>
          </label>
          -->

          <!--<label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.removeMailingAddresses" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Remove mailing addresses</div>
              <div class="text-sm muted">e.g., street names, apartment numbers, ZIP codes</div>
            </div>
          </label>
          -->

          <!--<label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.deleteIPAddresses" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Delete IP addresses</div>
              <div class="text-sm muted">e.g., IPv4 or IPv6 addresses</div>
            </div>
          </label>
          -->

          <label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.removeFinancialInfo" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Financial information</div>
              <div class="text-sm muted">e.g., credit card numbers, bank account numbers</div>
            </div>
          </label>

           <!--<label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.stripMedicalInfo" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Strip medical or health information</div>
              <div class="text-sm muted">e.g., diagnoses, treatment history</div>
            </div>
          </label>
          -->

          <!--<label class="flex items-start gap-3">
            <input type="checkbox" v-model="options.removeUsernames" class="accent-blue-500 h-4 w-4 mt-1" />
            <div>
              <div class="font-medium">Remove usernames or login credentials</div>
              <div class="text-sm muted">e.g., "admin123", "user@example.com"</div>
            </div>
          </label>
          -->
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
