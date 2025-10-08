<template>
  <div class="card p-0 flex flex-col h-[300px]">
    <div class="px-6 py-4 border-b border-slate-200 flex items-center justify-between">
      <h2 class="text-xl font-semibold">Sensitive Data</h2>
      <span class="text-sm text-slate-500">{{ items.length }}</span>
    </div>

    <div class="px-6 py-4 overflow-y-auto flex-1">
      <div v-if="items.length === 0" class="text-gray-500">
        No sensitive data found.
      </div>

      <ul v-else class="list-disc list-inside space-y-1 text-sm text-slate-700">
        <li v-for="(item, index) in items" :key="index">
          {{ item }}
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  items: string[]
}>()
</script>
