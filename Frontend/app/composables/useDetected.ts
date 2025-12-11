import { useState } from '#app'
import { watch } from 'vue'
import type { SensitiveItem } from '~/types'

const STORAGE_KEY = 'documentDetected'

function mapToRecord(map: Map<string, SensitiveItem[]>) {
  const obj: Record<string, SensitiveItem[]> = {}
  map.forEach((v, k) => obj[k] = v)
  return obj
}

function recordToMap(obj: Record<string, SensitiveItem[]>) {
  const m = new Map<string, SensitiveItem[]>()
  Object.keys(obj || {}).forEach(k => {
    const v = obj[k]
    if (v) m.set(k, v)
  })
  return m
}

export function useDetected() {
  const documentDetected = useState<Map<string, SensitiveItem[]>>(STORAGE_KEY, () => new Map())

  if (typeof window !== 'undefined') {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY)
      if (raw) {
        const parsed = JSON.parse(raw) as Record<string, SensitiveItem[]>
        documentDetected.value = recordToMap(parsed)
      }
    } catch (e) {

    }
  }

  function save() {
    try {
      if (typeof window === 'undefined') return
      const obj = mapToRecord(documentDetected.value)
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(obj))
    } catch (e) {

    }
  }

  try {
    watch(documentDetected, () => save(), { deep: true })
  } catch (e) {
   
  }

  function setDetected(docId: string, items: SensitiveItem[]) {
    const next = new Map(documentDetected.value)
    next.set(docId, items)
    documentDetected.value = next
    save()
  }

  function getDetected(docId: string): SensitiveItem[] {
    return documentDetected.value.get(docId) || []
  }

  function clearDetected(docId: string) {
    const next = new Map(documentDetected.value)
    next.delete(docId)
    documentDetected.value = next
    save()
  }

  return { setDetected, getDetected, clearDetected, documentDetected }
}