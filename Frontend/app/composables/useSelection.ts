import { useState } from '#app'

const STORAGE_KEY = 'selectedToHide'

type Inner = Record<string, string> // key -> value

function mapToRecord(m: Record<string, Inner>) { return m }

export function useSelection() {
  const sel = useState<Record<string, Inner>>(STORAGE_KEY, () => ({}))

  // try load from localStorage
  if (typeof window !== 'undefined') {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY)
      if (raw) sel.value = JSON.parse(raw) as Record<string, Inner>
    } catch { }
  }

  function save() {
    try {
      if (typeof window === 'undefined') return
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(sel.value))
    } catch { }
  }

  function toggle(docId: string, key: string, value: string) {
    if (!sel.value[docId]) sel.value[docId] = {}
    if (sel.value[docId][key]) {
      delete sel.value[docId][key]
    } else {
      sel.value[docId][key] = value
    }
    save()
  }

  function isSelected(docId: string, key: string) {
    return !!sel.value[docId] && !!sel.value[docId][key]
  }

  function getValues(docId: string) {
    const m = sel.value[docId] || {}
    return Object.values(m)
  }

  function clear(docId: string) {
    delete sel.value[docId]
    save()
  }

  function count(docId: string) {
    return Object.keys(sel.value[docId] || {}).length
  }

  return { toggle, isSelected, getValues, clear, count }
}
