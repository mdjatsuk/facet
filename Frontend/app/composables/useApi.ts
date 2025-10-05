export function useApi() {
  const config = useRuntimeConfig() 
  const base = config.public.apiBase

  async function get<T>(path: string): Promise<T> {
    return await $fetch<T>(`${base}${path}`)
  }

  async function upload<T>(path: string, file: File): Promise<T> {
    const form = new FormData()
    form.append('file', file)
    return await $fetch<T>(`${base}${path}`, { method: 'POST', body: form })
  }

  async function del<T>(path: string): Promise<T> {
    return await $fetch<T>(`${base}${path}`, { method: 'DELETE' })
  }

  return { get, upload, del }
}
