export type Bootstrap = {
  applicationName: string
  apiVersion: string
  supportedExtensions: string[]
  documentKinds: string[]
  fileStatuses: string[]
  reviewStatuses: string[]
  expenseCategories: string[]
  incomeCategories: string[]
}

export type IngestionJob = {
  id: string
  folderPath: string
  status: string
  startedAtUtc: string
  completedAtUtc: string | null
  totalFileCount: number
  completedFileCount: number
  failedFileCount: number
  needsReviewFileCount: number
}

export type ExtractedDocument = {
  id: string
  sourceFileId: string
  rawText: string
  tablesJson: string
  parserName: string
  parserWarningsJson: string
}

export type MappedDocument = {
  id: string
  sourceFileId: string
  documentKind: number
  direction: number
  suggestedCategory: string
  confidenceScore: number
  mappedDataJson: string
  validationIssuesJson: string
  matchResultsJson: string
  reviewStatus: number
}

export type SourceFile = {
  id: string
  ingestionJobId: string
  fileName: string
  absolutePath: string
  extension: string
  sizeBytes: number
  lastModifiedUtc: string
  status: number
  failureReason: string | null
  failureMessage: string | null
  extractedDocument: ExtractedDocument | null
  mappedDocument: MappedDocument | null
}

export type AccountingDocument = {
  id: string
  invoiceNumber: string
  documentKind: number
  direction: number
  counterpartyName: string
  currency: string
  totalAmount: number | null
  approvedCategory: string
  status: string
  confidenceScore: number
  approvedAtUtc: string | null
}

export type EditableMappedPayload = {
  documentKind: string
  direction: string
  counterpartyName: string
  counterpartyTaxIdentifier: string
  invoiceNumber: string
  invoiceDate: string
  dueDate: string
  currency: string
  subtotal: string
  taxAmount: string
  totalAmount: string
  category: string
}

export type ActivityItem = {
  id: string
  timestamp: string
  level: 'info' | 'success' | 'warning' | 'error'
  message: string
}
