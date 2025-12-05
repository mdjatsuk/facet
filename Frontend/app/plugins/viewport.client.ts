export default defineNuxtPlugin(() => {
  if (process.client) {
    const desired = 'width=device-width, initial-scale=1.0, maximum-scale=5.0, user-scalable=yes, viewport-fit=cover'
    let tag = document.querySelector('meta[name="viewport"]') as HTMLMetaElement | null
    if (!tag) {
      tag = document.createElement('meta')
      tag.name = 'viewport'
      document.head.appendChild(tag)
    }
    const current = tag!.getAttribute('content') || ''
    if (current !== desired) {
      tag!.setAttribute('content', desired)
    }

    // Lightweight debug: report effective width and UA once.
    const width = window.innerWidth
    const dpr = window.devicePixelRatio
    const ua = navigator.userAgent
    console.info('[FACET] viewport enforced:', desired)
    console.info('[FACET] width:', width, 'dpr:', dpr)
    console.info('[FACET] ua:', ua)
  }
})