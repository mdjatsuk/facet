<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="font-semibold">Sensitive Data</div>
      <div class="muted">{{ items.length }}</div>
    </div>

    <div class="listbox-body">
      <div v-if="items.length === 0" class="text-gray-500 p-4 text-center">
        No sensitive data found.
      </div>

      <ul v-else class="list-disc list-inside px-4 py-3 space-y-1 text-sm text-slate-700">
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
