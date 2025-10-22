export function useApi() {
  const config = useRuntimeConfig() 
  const base = config.public.apiBase
  function makeUrl(path: string) {
    if (!base) return path
    if (base.endsWith('/') && path.startsWith('/')) return base + path.substring(1)
    if (!base.endsWith('/') && !path.startsWith('/')) return base + '/' + path
    return base + path
  }

  async function get<T>(path: string): Promise<T> {
    return await $fetch<T>(makeUrl(path))
  }

  async function upload<T>(path: string, file: File): Promise<T> {
    const form = new FormData()
    form.append('file', file)
    return await $fetch<T>(makeUrl(path), { method: 'POST', body: form })
  }

  async function post<T>(path: string, body: any): Promise<T> {
    return await $fetch<T>(makeUrl(path), { method: 'POST', body })
  }

  async function del<T>(path: string): Promise<T> {
    return await $fetch<T>(makeUrl(path), { method: 'DELETE' })
  }

  async function postBlob(path: string, body: any): Promise<Blob> {
    const res = await $fetch(makeUrl(path), {
      method: 'POST',
      body: body,
      responseType: 'blob'
    })
    return res as Blob
  }

  return { get, upload, del, postBlob, post }
}
