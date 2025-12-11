<script setup lang="ts">
</script>

<template>
  <div class="min-h-screen bg-slate-50 flex items-center justify-center px-4 sm:px-6 relative">
    <!-- Language Switcher (shared component) -->
    <div class="fixed top-4 right-4 z-50">
      <LanguageSwitcher variant="blue" />
    </div>

    <div class="max-w-2xl w-full">
      <div class="mb-6 sm:mb-8 text-center">
        <h1 class="text-3xl sm:text-4xl font-semibold">{{ $t('login.title') }}</h1>
        <p class="mt-2 sm:mt-3 text-sm sm:text-base text-slate-600">{{ $t('login.description') }}</p>
      </div>

      <ClientOnly>
        <LoginForm />
      </ClientOnly>
      
      <div class="text-center mt-4">
        <a href="/register" class="text-sm text-slate-600 underline">{{ $t('login.createAccount') }}</a>
      </div>
    </div>
  </div>
</template>
