import { useState } from '#app'

const STORAGE_KEY = 'selectedToHide'

type Inner = Record<string, string>

export function useSelection() {
  const sel = useState<Record<string, Inner>>(STORAGE_KEY, () => ({}))

  if (process.client) {
    try {
      const raw = window.localStorage.getItem(STORAGE_KEY)
      if (raw) sel.value = JSON.parse(raw) as Record<string, Inner>
    } catch { }
  }

  function save() {
    try {
      if (!process.client) return
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(sel.value))
    } catch { }
  }

  function ensureDoc(docId?: string | null) {
    if (!docId) return
    if (!sel.value[docId]) sel.value[docId] = {}
  }

  function toggle(docId: string | undefined | null, key: string, value: string) {
    if (!docId) return
    ensureDoc(docId)
    const doc = sel.value[docId]!
    if (doc[key]) delete doc[key]
    else doc[key] = value
    save()
  }

  function isSelected(docId: string | undefined | null, key: string) {
    if (!docId) return false
    const doc = sel.value[docId]
    return !!doc && !!doc[key]
  }

  function getValues(docId: string | undefined | null) {
    if (!docId) return [] as string[]
    return Object.values(sel.value[docId] || {})
  }

  function clear(docId: string | undefined | null) {
    if (!docId) return
    delete sel.value[docId]
    save()
  }

  function count(docId: string | undefined | null) {
    if (!docId) return 0
    return Object.keys(sel.value[docId] || {}).length
  }

  function setAll(docId: string | undefined | null, values: string[]) {
    if (!docId) return
    ensureDoc(docId)
    const doc = sel.value[docId]!
    for (const v of values) doc[v] = v
    save()
  }

  function toggleAll(docId: string | undefined | null, values: string[]) {
    if (!docId) return
    const curr = getValues(docId)
    const allSelected = values.length > 0 && values.every(v => curr.includes(v))
    if (allSelected) {
      const doc = sel.value[docId]
      if (doc) {
        for (const v of values) delete doc[v]
        if (Object.keys(doc).length === 0) delete sel.value[docId]
      }
    } else {
      setAll(docId, values)
    }
    save()
  }

  return { toggle, isSelected, getValues, clear, count, setAll, toggleAll }
}
