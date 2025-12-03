<template>
  <div class="min-h-screen bg-slate-50 flex items-center justify-center px-4 sm:px-6">
    <div class="max-w-2xl w-full">
      <div class="mb-6 sm:mb-8 text-center">
        <h1 class="text-3xl sm:text-4xl font-semibold">Welcome to FACET</h1>
        <p class="mt-2 sm:mt-3 text-sm sm:text-base text-slate-600">FACET is a simple tool for uploading documents and detecting sensitive data. Upload a file, apply a policy, and get a redacted copy.</p>
      </div>

      <ClientOnly>
        <LoginForm />
      </ClientOnly>
      
      <div class="text-center mt-4">
        <a href="/register" class="text-sm text-slate-600 underline">Create account</a>
      </div>
    </div>
  </div>
</template>
