export interface Document {
  id: string
  fileName: string
  contentType: string
  sizeBytes: number
  uploadedAt: string
}

export interface UploadResult {
  success: boolean
  message: string
  document?: Document
}

export interface ApiError {
  data?: string
  message?: string
}