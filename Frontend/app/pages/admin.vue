<template>
  <div class="p-6 max-w-xl mx-auto">
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-2xl font-bold">Manage users</h1>
      <button @click="navigateBack" class="px-3 py-1 bg-slate-50 hover:bg-slate-100 text-slate-700 rounded">← Back</button>
    </div>

    <div v-if="!isAdmin" class="text-red-600">You are not authorized to view this page.</div>

    <div v-if="isAdmin" class="space-y-4">
      <div>
        <label class="block mb-2">Username</label>
        <input v-model="username" @keyup.enter="fetchUser" placeholder="Type username and press Enter" class="w-full p-2 border rounded" />
        <div class="text-xs text-slate-500 mt-1">Press Enter to load user info</div>
      </div>

  <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block mb-2">Set role</label>
          <select v-model="role" class="w-full p-2 border rounded mb-2">
            <option value="User">User</option>
            <option value="Admin">Admin</option>
          </select>
          <button @click="setRole" :disabled="loading || !username" class="w-full px-3 py-2 bg-blue-600 text-white rounded">Set Role</button>
        </div>

        <div>
          <label class="block mb-2">Ban / Unban</label>
          <div class="flex gap-2">
            <button @click="banUser" :disabled="loading || !username" class="flex-1 px-3 py-2 bg-red-600 hover:bg-red-700 text-white rounded">Ban user</button>
            <button @click="unbanUser" :disabled="loading || !username" class="flex-1 px-3 py-2 bg-emerald-600 hover:bg-emerald-700 text-white rounded">Unban user</button>
          </div>
          <div class="text-xs text-slate-500 mt-1">Banned users cannot log in until unbanned.</div>
        </div>
      </div>

      <div>
        <label class="block mb-2">Delete account</label>
        <button @click="onDelete" :disabled="loading || !username" class="w-full px-3 py-2 bg-red-700 hover:bg-red-800 text-white rounded">Delete user (permanent)</button>
      </div>

      <div v-if="message" class="mt-2" :class="{ 'text-green-600': success, 'text-red-600': !success }">{{ message }}</div>

      <div v-if="userInfo" class="mt-4 p-4 border rounded bg-white shadow-sm">
        <div class="flex items-center justify-between">
          <div>
            <div class="text-sm text-slate-600">User info</div>
            <div class="font-medium text-lg">{{ userInfo.username }}</div>
          </div>
          <div class="text-sm text-slate-500">ID: {{ userInfo.id }}</div>
        </div>
        <div class="mt-3 grid grid-cols-2 gap-3 text-sm">
          <div>Role: <span class="font-medium">{{ userInfo.role }}</span></div>
          <div>Status: <span :class="userInfo.isBanned ? 'text-red-600 font-medium' : 'text-emerald-600 font-medium'">{{ userInfo.isBanned ? 'Banned' : 'Active' }}</span></div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useAuth } from '~/composables/useAuth'

const { currentRole, fetchWithToken } = useAuth() as any
const username = ref('')
const role = ref('User')
const loading = ref(false)
const message = ref('')
const success = ref(false)

const isAdmin = computed(() => currentRole?.value === 'Admin')

onMounted(() => {
  if (!isAdmin.value) {
    // Optionally redirect away
    // navigateTo('/')
  }
})

const navigateBack = () => navigateTo('/')

const userInfo = ref<any | null>(null)

const fetchUser = async () => {
  if (!username.value) return
  loading.value = true
  message.value = ''
  success.value = false
  userInfo.value = null
  try {
    const info = await fetchWithToken(`/api/users/manage/${encodeURIComponent(username.value)}`)
    userInfo.value = info
    // prefill role with fetched role
    role.value = info.role || 'User'
    message.value = 'User loaded'
    success.value = true
  } catch (e: any) {
    message.value = e?.data?.message || 'Failed to load user'
    success.value = false
  } finally {
    loading.value = false
  }
}

const setRole = async () => {
  loading.value = true
  message.value = ''
  success.value = false
  try {
    await fetchWithToken('/api/users/manage/set-role', { method: 'POST', body: { username: username.value, role: role.value } })
    message.value = 'Role updated successfully'
    success.value = true
  } catch (e: any) {
    message.value = e?.data?.message || 'Failed to update role'
    success.value = false
  } finally {
    loading.value = false
  }
}

const banUser = async () => {
  loading.value = true
  message.value = ''
  success.value = false
  try {
    await fetchWithToken('/api/users/manage/ban', { method: 'POST', body: { username: username.value, ban: true } })
    message.value = 'User banned'
    success.value = true
  } catch (e: any) {
    message.value = e?.data?.message || 'Failed to ban user'
    success.value = false
  } finally {
    loading.value = false
  }
}

const unbanUser = async () => {
  loading.value = true
  message.value = ''
  success.value = false
  try {
    await fetchWithToken('/api/users/manage/ban', { method: 'POST', body: { username: username.value, ban: false } })
    message.value = 'User unbanned'
    success.value = true
  } catch (e: any) {
    message.value = e?.data?.message || 'Failed to unban user'
    success.value = false
  } finally {
    loading.value = false
  }
}

const onDelete = async () => {
  if (!confirm(`Delete account for user '${username.value}'? This cannot be undone.`)) return
  loading.value = true
  message.value = ''
  success.value = false
  try {
    await fetchWithToken(`/api/users/manage/${encodeURIComponent(username.value)}`, { method: 'DELETE' })
    message.value = 'User deleted'
    success.value = true
  } catch (e: any) {
    message.value = e?.data?.message || 'Failed to delete user'
    success.value = false
  } finally {
    loading.value = false
  }
}
</script>
