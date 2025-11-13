export function useApi() {
  const config = useRuntimeConfig() 
  const base = config.public.apiBase
  const tokenState = useState<string | undefined>('token')
  function makeUrl(path: string) {
    if (!base) return path
    if (base.endsWith('/') && path.startsWith('/')) return base + path.substring(1)
    if (!base.endsWith('/') && !path.startsWith('/')) return base + '/' + path
    return base + path
  }

  async function get<T>(path: string): Promise<T> {
    const headers: Record<string,string> = {}
    if (tokenState.value) headers['Authorization'] = `Bearer ${tokenState.value}`
    return await $fetch<T>(makeUrl(path), { headers })
  }

  async function upload<T>(path: string, file: File): Promise<T> {
    const form = new FormData()
    form.append('file', file)
    const headers: Record<string,string> = {}
    if (tokenState.value) headers['Authorization'] = `Bearer ${tokenState.value}`
    return await $fetch<T>(makeUrl(path), { method: 'POST', body: form, headers })
  }

  async function post<T>(path: string, body: any): Promise<T> {
    const headers: Record<string,string> = { 'Content-Type': 'application/json' }
    if (tokenState.value) headers['Authorization'] = `Bearer ${tokenState.value}`
    return await $fetch<T>(makeUrl(path), { method: 'POST', body, headers })
  }

  async function del<T>(path: string): Promise<T> {
    const headers: Record<string,string> = {}
    if (tokenState.value) headers['Authorization'] = `Bearer ${tokenState.value}`
    return await $fetch<T>(makeUrl(path), { method: 'DELETE', headers })
  }

  async function postBlob(path: string, body: any): Promise<Blob> {
    const headers: Record<string,string> = {}
    if (tokenState.value) headers['Authorization'] = `Bearer ${tokenState.value}`
    const res = await $fetch(makeUrl(path), {
      method: 'POST',
      body: body,
      responseType: 'blob',
      headers
    })
    return res as Blob
  }

  async function customFetch<T>(path: string, options?: any): Promise<T> {
    const headers = { ...(options?.headers || {}) } as Record<string,string>
    if (tokenState.value) headers['Authorization'] = `Bearer ${tokenState.value}`
    return await $fetch<T>(makeUrl(path), { ...(options || {}), headers })
  }

  return { get, upload, del, postBlob, post, customFetch }
}
