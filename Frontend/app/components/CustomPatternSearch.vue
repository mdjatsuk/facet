<template>
  <div class="listbox">
    <div class="listbox-header">
      <div class="flex items-center gap-2 sm:gap-3">
        <svg class="w-4 h-4 sm:w-5 sm:h-5 text-primary flex-shrink-0" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
        </svg>
        <div>
          <div class="text-sm sm:text-base font-semibold">{{ $t('customPattern.title') }}</div>
          <div class="text-xs text-slate-400">{{ $t('customPattern.description') }}</div>
        </div>
      </div>
    </div>

    <div class="listbox-body">
      <div class="px-3 py-3">
        <div class="flex flex-col gap-2">
          <input
            v-model="customPattern"
            type="text"
            :placeholder="$t('customPattern.placeholder')"
            class="w-full px-3 py-2 text-sm border border-slate-300 rounded focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary"
            @keyup.enter="scanCustomPattern"
          />
          <button
            @click="scanCustomPattern"
            :disabled="!customPattern.trim() || isScanning"
            class="w-full px-4 py-2 bg-primary text-white rounded text-sm hover:bg-primary/90 disabled:opacity-50 disabled:cursor-not-allowed transition-colors touch-manipulation"
          >
            {{ isScanning ? 'Scanning...' : $t('customPattern.button') }}
          </button>
          <div v-if="scanError" class="text-xs text-red-600 px-1">{{ scanError }}</div>
          <div v-if="scanSuccess" class="text-xs text-green-600 px-1">
            {{ scanSuccess }}
            <div class="text-xs text-slate-500 mt-1">{{ $t('customPattern.note') }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, toRef, computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { SensitiveItem } from '~/types'

const { t } = useI18n()

const props = defineProps<{
  docId?: string | null
}>()

const docIdRef = toRef(props, 'docId')
const docId = computed(() => docIdRef.value || '')

const customPattern = ref('')
const isScanning = ref(false)
const scanError = ref('')
const scanSuccess = ref('')

const config = useRuntimeConfig()
const apiBase = config.public.apiBase as string
const tokenState = useState<string | undefined>('token')
const { setDetected, getDetected } = useDetected()

const emit = defineEmits<{
  patternFound: [type: string]
}>()

async function scanCustomPattern() {
  if (!customPattern.value.trim()) return
  if (!docId.value) {
    scanError.value = 'No document selected'
    setTimeout(() => scanError.value = '', 3000)
    return
  }

  isScanning.value = true
  scanError.value = ''
  scanSuccess.value = ''

  try {
    const pattern = customPattern.value.trim()
    
    // First, check if there are already detected items that contain this pattern
    const existing = getDetected(docId.value) || []
    const matchingItems: SensitiveItem[] = []
    
    // Search through existing detected items for matches
    for (const item of existing) {
      if (item.value && item.value.toLowerCase().includes(pattern.toLowerCase())) {
        // Found an existing item that contains the pattern
        matchingItems.push({
          type: `Custom: "${pattern}"`,
          value: item.value,  // Use the full value, not just the pattern
          // For custom patterns, always use -1 to indicate value-based matching
          indexStart: -1,
          indexEnd: -1
        })
      }
    }
    
    // If we found matches in existing items, use those
    if (matchingItems.length > 0) {
      // Don't add duplicates - only add the new custom type entries
      // Filter out existing entries that match the custom pattern matches
      const filtered = existing.filter(item => {
        // Keep items that don't match this pattern, or items that already have the custom type for this pattern
        const matches = matchingItems.some(match => match.value === item.value)
        const isAlreadyCustomType = item.type === `Custom: "${pattern}"`
        return !matches || isAlreadyCustomType
      })
      
      // Also remove any duplicate custom type entries for this pattern
      const deduped = filtered.filter((item, idx, arr) => {
        if (item.type !== `Custom: "${pattern}"`) return true
        // For custom types, only keep the first occurrence of each value
        return arr.findIndex(i => i.type === `Custom: "${pattern}"` && i.value === item.value) === idx
      })
      
      const merged = [...deduped, ...matchingItems]
      setDetected(docId.value, merged)

      const customType = `Custom: "${pattern}"`
      emit('patternFound', customType)

      scanSuccess.value = t('customPattern.foundMatches', { count: matchingItems.length, pattern })
      customPattern.value = ''
      setTimeout(() => scanSuccess.value = '', 3000)
      return
    }
    
    // If not found in existing items, search in document text
    const previewUrl = `${apiBase}/documents/${docId.value}/preview`
    const headers: Record<string, string> = { 
      Authorization: `Bearer ${tokenState.value || ''}` 
    }
    
    let textContent = ''
    
    try {
      const previewResp = await fetch(previewUrl, { method: 'GET', headers })
      if (previewResp.ok) {
        const htmlContent = await previewResp.text()
        // Extract text from HTML
        const parser = new DOMParser()
        const doc = parser.parseFromString(htmlContent, 'text/html')
        textContent = doc.body.textContent || doc.body.innerText || ''
      }
    } catch (e) {
      // If preview fails, try to fetch the file directly (for text files)
      const fileUrl = `${apiBase}/documents/${docId.value}/file`
      const fileResp = await fetch(fileUrl, { method: 'GET', headers })
      
      if (!fileResp.ok) {
        throw new Error(`Failed to fetch document: ${fileResp.status}`)
      }
      
      const blob = await fileResp.blob()
      // Only try to read as text if it's a text file
      if (blob.type.startsWith('text/')) {
        textContent = await blob.text()
      } else {
        throw new Error('Unable to extract text from this document type')
      }
    }

    if (!textContent || textContent.trim().length === 0) {
      throw new Error('No text content found in document')
    }

    // Search for words/tokens containing the pattern
    const lowerPattern = pattern.toLowerCase()
    
    // Split text into lines to avoid matching across paragraphs/line breaks
    const lines = textContent.split(/\r?\n/)
    
    // Match alphanumeric words only (excluding symbols like :, !, etc.)
    const wordBoundaryRegex = /\b[\w]+\b/g
    const matches: SensitiveItem[] = []
    const seen = new Set<string>()
    
    let currentIndex = 0
    for (const line of lines) {
      const lineLower = line.toLowerCase()
      
      let match
      wordBoundaryRegex.lastIndex = 0 // Reset regex
      while ((match = wordBoundaryRegex.exec(line)) !== null) {
        const word = match[0]
        const wordLower = word.toLowerCase()
        
        if (wordLower.includes(lowerPattern)) {
          // Avoid duplicates
          if (!seen.has(word)) {
            seen.add(word)
            matches.push({
              type: `Custom: "${pattern}"`,
              value: word,
              // For custom patterns found by text search, use -1 to indicate these are text-based matches
              // The backend should handle custom pattern types specially
              indexStart: -1,
              indexEnd: -1
            })
          }
        }
      }
      
      // Move index forward to avoid matching the same line again
      currentIndex += line.length + 1
    }

    if (matches.length > 0) {
      // Get existing detected items and merge
      const merged = [...existing, ...matches]
      setDetected(docId.value, merged)

      // Emit event to notify parent about new type
      const customType = `Custom: "${pattern}"`
      emit('patternFound', customType)

      scanSuccess.value = t('customPattern.foundMatches', { count: matches.length, pattern })
      customPattern.value = ''
      setTimeout(() => scanSuccess.value = '', 3000)
    } else {
      scanError.value = t('customPattern.noMatches', { pattern })
      setTimeout(() => scanError.value = '', 3000)
    }

  } catch (error) {
    console.error('Custom pattern scan error:', error)
    scanError.value = error instanceof Error ? error.message : 'Failed to scan document'
    setTimeout(() => scanError.value = '', 5000)
  } finally {
    isScanning.value = false
  }
}
</script>
