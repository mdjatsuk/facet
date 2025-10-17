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

export interface Policy {
  id: string
  name: string
  options: {
    deleteAllEmails: boolean
    removePhoneNumbers: boolean
    removeNationalIds: boolean
    anonymizeNames: boolean
    removeMailingAddresses: boolean
    deleteIPAddresses: boolean
    removeFinancialInfo: boolean
    stripMedicalInfo: boolean
    removeUsernames: boolean
  }
  createdAt: string
}