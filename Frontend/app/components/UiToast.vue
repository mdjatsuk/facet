<script setup lang="ts">

const props = defineProps<{ type?: 'success'|'error'|'info', message: string }>()
const color = computed(() => props.type === 'success' ? 'bg-emerald-50 text-emerald-700 border-emerald-200' :
                       props.type === 'error' ? 'bg-rose-50 text-rose-700 border-rose-200' :
                       'bg-sky-50 text-sky-700 border-sky-200')
</script>
<template>
  <div :class="['rounded-xl border px-4 py-3 shadow-soft', color]">{{ message }}</div>
</template>
