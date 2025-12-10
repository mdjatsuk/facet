<template>
  <div class="max-w-md mx-auto mt-10 sm:mt-20 p-4 sm:p-6 bg-white rounded-lg shadow">
    <form class="space-y-4" @submit.prevent="submit">
      <div>
        <label class="block text-sm font-medium text-slate-700">{{ $t('login.user') }}</label>
        <input v-model="user.username" class="mt-1 block w-full border rounded px-3 py-2.5 sm:py-2 text-base" autocomplete="username" />
      </div>

      <div>
        <label class="block text-sm font-medium text-slate-700">{{ $t('login.password') }}</label>
        <input v-model="user.password" type="password" class="mt-1 block w-full border rounded px-3 py-2.5 sm:py-2 text-base" autocomplete="current-password" />
      </div>

      <div class="flex items-center justify-between">
        <button type="submit" class="px-4 py-2.5 sm:py-2 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white rounded touch-manipulation text-base">{{ $t('login.submit') }}</button>
      </div>

        <div v-if="showError || authError" class="text-sm text-red-600">{{ authError || 'Login failed — please check credentials.' }}</div>
    </form>
  </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useAuth } from "~/composables/useAuth";
import type { User } from "~/types/user";

const { t } = useI18n();
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
