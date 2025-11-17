<template>
  <div class="min-h-screen">
    <ClientOnly>
      <div class="flex items-center justify-center bg-slate-50 min-h-screen">
        <div class="max-w-2xl w-full px-6">
      <div class="mb-8 text-center">
        <h1 class="text-4xl font-semibold">Welcome to FACET</h1>
        <p class="mt-3 text-slate-600">FACET is a simple tool for uploading documents and detecting sensitive data. Upload a file, apply a policy, and get a redacted copy.</p>
      </div>

      <LoginForm />
      <div class="text-center mt-4">
        <a href="/register" class="text-sm text-slate-600 underline">Create account</a>
      </div>
        </div>
      </div>
    </ClientOnly>
  </div>
</template>
