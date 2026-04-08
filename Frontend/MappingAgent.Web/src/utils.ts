import type { Bootstrap, EditableMappedPayload, SourceFile } from './types'

const fileStatusFallback = [
  'Discovered',
  'Queued',
  'Parsing',
  'Classifying',
  'Extracting',
  'Matching',
  'Needs Review',
  'Completed',
  'Failed',
]

export function statusLabel(status: number, bootstrap: Bootstrap | null): string {
  return bootstrap?.fileStatuses[status] ?? fileStatusFallback[status] ?? `Status ${status}`
}

export function directionLabel(direction: number): string {
  return direction === 2 ? 'Income' : direction === 1 ? 'Expense' : 'Unknown'
}

export function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function formatUtc(value: string): string {
  return new Date(value).toLocaleString()
}

export function parseJson(value: string | undefined): unknown {
  if (!value) {
    return null
  }

  try {
    return JSON.parse(value)
  } catch {
    return value
  }
}

export function parseArray(value: string | undefined): string[] {
  const parsed = parseJson(value)
  return Array.isArray(parsed) ? parsed.map((item) => String(item)) : []
}

export function prettyJson(value: unknown): string {
  if (!value) {
    return 'No data'
  }

  if (typeof value === 'string') {
    try {
      return JSON.stringify(JSON.parse(value), null, 2)
    } catch {
      return value
    }
  }

  return JSON.stringify(value, null, 2)
}

export function buildEditablePayload(file: SourceFile | null): EditableMappedPayload | null {
  if (!file?.mappedDocument) {
    return null
  }

  const mapped = parseJson(file.mappedDocument.mappedDataJson) as Record<string, unknown> | null
  const counterparty = getObject(mapped?.counterparty)
  const invoice = getObject(mapped?.invoice)

  return {
    documentKind: readString(mapped?.documentKind),
    direction: readString(mapped?.direction),
    counterpartyName: readString(counterparty?.counterpartyName),
    counterpartyTaxIdentifier: readString(counterparty?.counterpartyTaxIdentifier),
    invoiceNumber: readString(invoice?.invoiceNumber),
    invoiceDate: readString(invoice?.invoiceDate),
    dueDate: readString(invoice?.dueDate),
    currency: readString(invoice?.currency),
    subtotal: readNumeric(invoice?.subtotal),
    taxAmount: readNumeric(invoice?.taxAmount),
    totalAmount: readNumeric(invoice?.totalAmount),
    category: readString(mapped?.category) || file.mappedDocument.suggestedCategory,
  }
}

export function parseNumericField(value: string): number | null {
  if (!value.trim()) {
    return null
  }

  const numeric = Number(value)
  return Number.isFinite(numeric) ? numeric : null
}

function getObject(value: unknown): Record<string, unknown> | null {
  return value && typeof value === 'object' && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : null
}

function readString(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

function readNumeric(value: unknown): string {
  return typeof value === 'number' ? value.toString() : ''
}
