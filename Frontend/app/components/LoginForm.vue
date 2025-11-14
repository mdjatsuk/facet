<template>
  <div class="max-w-md mx-auto mt-20 p-6 bg-white rounded-lg shadow">
    <form class="space-y-4" @submit.prevent="submit">
      <div>
        <label class="block text-sm font-medium text-slate-700">User</label>
        <input v-model="user.username" class="mt-1 block w-full border rounded px-3 py-2" />
      </div>

      <div>
        <label class="block text-sm font-medium text-slate-700">Password</label>
        <input v-model="user.password" type="password" class="mt-1 block w-full border rounded px-3 py-2" />
      </div>

      <div class="flex items-center justify-between">
        <button type="submit" class="px-4 py-2 bg-blue-600 text-white rounded">Submit</button>
      </div>

        <div v-if="showError || authError" class="text-sm text-red-600">{{ authError || 'Login failed — please check credentials.' }}</div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useAuth } from "~/composables/useAuth";
import type { User } from "~/types/user";

const auth = useAuth();
const user: User = { username: "", password: "" };

let showError = ref(false);
const authError = computed(() => (auth as any).authError?.value || '');

const submit = async () => {
  const ok = await auth.logIn(user);
  showError.value = !ok;
  // auth.authError is populated by the composable on banned/login errors
  // ensure UI updates
  // navigate away only if ok
  if (ok) {
    await navigateTo("/");
  }
};
</script>
