export interface Document {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedAt: string
}

export interface SensitiveItem {
  type: string
  value: string
  indexStart: number
  indexEnd: number
}

export interface UploadResult {
  success: boolean
  message: string
  document?: Document
  detected?: SensitiveItem[]
}

export interface ApiError {
  data?: string
  message?: string
}