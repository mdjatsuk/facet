<template>
  <div class="min-h-screen">
    <ClientOnly>
      <div class="flex items-center justify-center bg-slate-50 min-h-screen">
        <div class="max-w-md w-full px-6">
      <div class="mb-6 text-center">
        <h1 class="text-2xl font-semibold">Create account</h1>
        <p class="text-sm text-slate-600">Create an account to sign in and use FACET.</p>
      </div>

      <form class="space-y-4" @submit.prevent="submit">
        <div>
          <label class="block text-sm font-medium text-slate-700">User</label>
          <input v-model="user.username" class="mt-1 block w-full border rounded px-3 py-2" />
        </div>

        <div>
          <label class="block text-sm font-medium text-slate-700">Password</label>
          <div class="mt-1 relative">
            <input v-model="user.password" :type="showPassword ? 'text' : 'password'" class="block w-full border rounded px-3 py-2 pr-10" />
          </div>
        </div>

        <div class="flex items-center justify-between">
          <button type="submit" :disabled="submitting" class="px-4 py-2 bg-emerald-600 text-white rounded disabled:opacity-50">Create account</button>
          <NuxtLink to="/login" class="text-sm text-slate-600 underline">Back to login</NuxtLink>
        </div>

        <div v-if="message" class="text-sm text-red-600">{{ message }}</div>
      </form>
        </div>
      </div>
    </ClientOnly>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import type { User } from '~/types/user'
const user = ref<User>({ username: '', password: '' })
const message = ref<string | null>(null)
const submitting = ref(false)
const api = useApi()
const auth = useAuth()

const showPassword = ref(false)

const validate = (u: User) => {
  if (!u.username || u.username.trim().length < 3) return 'Username must be at least 3 characters'
  if (/^\d+$/.test(u.username)) return 'Username cannot be only digits'
  if (!u.password || u.password.length < 5) return 'Password must be at least 5 characters'
  if (!/[A-Za-z]/.test(u.password) || !/\d/.test(u.password)) return 'Password must contain at least one letter and one digit'
  return null
}

const submit = async () => {
  message.value = null
  const v = validate(user.value)
  if (v) { message.value = v; return }
  submitting.value = true
  try {
    const ok = await auth.register(user.value)
    if (ok) {
      await navigateTo('/')
      return
    }
    await navigateTo('/login')
  }
  catch (e: any) {
    const err = e?.data || e?.message || 'Registration failed'
    message.value = typeof err === 'string' ? err : (err.message || JSON.stringify(err))
  }
  finally {
    submitting.value = false
  }
}
</script>
