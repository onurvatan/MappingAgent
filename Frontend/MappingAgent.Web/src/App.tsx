import { FormEvent, useEffect, useMemo, useState } from 'react'
import './App.css'

type Bootstrap = {
  applicationName: string
  apiVersion: string
  supportedExtensions: string[]
  documentKinds: string[]
  fileStatuses: string[]
  reviewStatuses: string[]
  expenseCategories: string[]
  incomeCategories: string[]
}

type IngestionJob = {
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

type ExtractedDocument = {
  id: string
  sourceFileId: string
  rawText: string
  tablesJson: string
  parserName: string
  parserWarningsJson: string
}

type MappedDocument = {
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

type SourceFile = {
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

type AccountingDocument = {
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

const demoFolder = 'c:\\Users\\Techp\\git\\MappingAgent\\DemoData\\Phase4Sample'

function App() {
  const [bootstrap, setBootstrap] = useState<Bootstrap | null>(null)
  const [jobs, setJobs] = useState<IngestionJob[]>([])
  const [files, setFiles] = useState<SourceFile[]>([])
  const [savedDocuments, setSavedDocuments] = useState<AccountingDocument[]>([])
  const [folderPath, setFolderPath] = useState(demoFolder)
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [selectedFileId, setSelectedFileId] = useState<string | null>(null)
  const [busyAction, setBusyAction] = useState<'scan' | 'parse' | 'map' | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void Promise.all([
      fetchJson<Bootstrap>('/api/meta/bootstrap'),
      fetchJson<IngestionJob[]>('/api/ingestion-jobs'),
      fetchJson<AccountingDocument[]>('/api/accounting-documents'),
    ])
      .then(([bootstrapResponse, jobsResponse, accountingDocuments]) => {
        setBootstrap(bootstrapResponse)
        setJobs(jobsResponse)
        setSavedDocuments(accountingDocuments)
        if (jobsResponse.length > 0) {
          setSelectedJobId(jobsResponse[0].id)
        }
      })
      .catch((fetchError: Error) => {
        setError(fetchError.message)
      })
  }, [])

  useEffect(() => {
    if (!selectedJobId) {
      setFiles([])
      return
    }

    void fetchJson<SourceFile[]>(`/api/ingestion-jobs/${selectedJobId}/files`)
      .then((response) => {
        setFiles(response)
        if (!selectedFileId || !response.some((file) => file.id === selectedFileId)) {
          setSelectedFileId(response[0]?.id ?? null)
        }
      })
      .catch((fetchError: Error) => {
        setError(fetchError.message)
      })
  }, [selectedJobId])

  const selectedJob = useMemo(
    () => jobs.find((job) => job.id === selectedJobId) ?? null,
    [jobs, selectedJobId],
  )

  const selectedFile = useMemo(
    () => files.find((file) => file.id === selectedFileId) ?? null,
    [files, selectedFileId],
  )

  async function refreshJobsAndDocuments(preferredJobId?: string) {
    const [jobsResponse, documentsResponse] = await Promise.all([
      fetchJson<IngestionJob[]>('/api/ingestion-jobs'),
      fetchJson<AccountingDocument[]>('/api/accounting-documents'),
    ])

    setJobs(jobsResponse)
    setSavedDocuments(documentsResponse)

    const nextJobId =
      preferredJobId && jobsResponse.some((job) => job.id === preferredJobId)
        ? preferredJobId
        : jobsResponse[0]?.id ?? null

    setSelectedJobId(nextJobId)
  }

  async function refreshFiles(jobId: string) {
    const response = await fetchJson<SourceFile[]>(`/api/ingestion-jobs/${jobId}/files`)
    setFiles(response)
    setSelectedFileId((current) =>
      current && response.some((file) => file.id === current) ? current : response[0]?.id ?? null,
    )
  }

  async function handleScan(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setBusyAction('scan')

    try {
      const response = await postJson<{ ingestionJobId: string }>('/api/folders/scan', {
        folderPath,
      })

      await refreshJobsAndDocuments(response.ingestionJobId)
      await refreshFiles(response.ingestionJobId)
    } catch (scanError) {
      setError(getErrorMessage(scanError))
    } finally {
      setBusyAction(null)
    }
  }

  async function handleParse() {
    if (!selectedJobId) {
      return
    }

    setError(null)
    setBusyAction('parse')

    try {
      await postJson(`/api/ingestion-jobs/${selectedJobId}/parse`)
      await refreshJobsAndDocuments(selectedJobId)
      await refreshFiles(selectedJobId)
    } catch (parseError) {
      setError(getErrorMessage(parseError))
    } finally {
      setBusyAction(null)
    }
  }

  async function handleMap() {
    if (!selectedJobId) {
      return
    }

    setError(null)
    setBusyAction('map')

    try {
      await postJson(`/api/ingestion-jobs/${selectedJobId}/map`)
      await refreshJobsAndDocuments(selectedJobId)
      await refreshFiles(selectedJobId)
    } catch (mapError) {
      setError(getErrorMessage(mapError))
    } finally {
      setBusyAction(null)
    }
  }

  return (
    <main className="app-shell">
      <section className="hero">
        <div>
          <p className="eyebrow">Phase 5 Workspace</p>
          <h1>{bootstrap?.applicationName ?? 'MappingAgent'}</h1>
          <p className="lede">
            Folder-based accounting intake with scan, parse, mapping, review, and saved-record views.
          </p>
        </div>
        <div className="hero-meta">
          <span>Version {bootstrap?.apiVersion ?? 'loading'}</span>
          <span>Types {bootstrap?.supportedExtensions.join(', ') ?? '...'}</span>
        </div>
      </section>

      <section className="workspace-grid">
        <article className="panel action-panel">
          <h2>Folder Intake</h2>
          <form className="folder-form" onSubmit={handleScan}>
            <label htmlFor="folderPath">Folder path</label>
            <input
              id="folderPath"
              value={folderPath}
              onChange={(event) => setFolderPath(event.target.value)}
              placeholder="C:\\Accounting\\Incoming"
            />
            <div className="button-row">
              <button type="submit" disabled={busyAction === 'scan'}>
                {busyAction === 'scan' ? 'Scanning...' : 'Scan Folder'}
              </button>
              <button type="button" onClick={handleParse} disabled={!selectedJobId || busyAction !== null}>
                Parse Job
              </button>
              <button type="button" onClick={handleMap} disabled={!selectedJobId || busyAction !== null}>
                Map Job
              </button>
            </div>
          </form>
          <p className="hint">
            Demo path: <code>{demoFolder}</code>
          </p>
          {error && <p className="error">{error}</p>}
        </article>

        <article className="panel">
          <h2>Jobs</h2>
          <div className="job-list">
            {jobs.map((job) => (
              <button
                key={job.id}
                type="button"
                className={`job-card ${job.id === selectedJobId ? 'selected' : ''}`}
                onClick={() => setSelectedJobId(job.id)}
              >
                <span className="job-status">{job.status}</span>
                <strong>{job.folderPath}</strong>
                <span>
                  {job.totalFileCount} files, {job.completedFileCount} completed, {job.needsReviewFileCount} review,{' '}
                  {job.failedFileCount} failed
                </span>
              </button>
            ))}
            {jobs.length === 0 && <p>No ingestion jobs yet.</p>}
          </div>
        </article>
      </section>

      <section className="workspace-grid">
        <article className="panel">
          <h2>Files</h2>
          <div className="file-list">
            {files.map((file) => (
              <button
                key={file.id}
                type="button"
                className={`file-row ${file.id === selectedFileId ? 'selected' : ''}`}
                onClick={() => setSelectedFileId(file.id)}
              >
                <span className={`status-dot status-${file.status}`}></span>
                <div>
                  <strong>{file.fileName}</strong>
                  <span>{statusLabel(file.status, bootstrap)}</span>
                </div>
              </button>
            ))}
            {selectedJob && files.length === 0 && <p>No files loaded for this job.</p>}
          </div>
        </article>

        <article className="panel detail-panel">
          <h2>Review Workspace</h2>
          {!selectedFile && <p>Select a file to inspect parsed and mapped output.</p>}
          {selectedFile && (
            <div className="detail-stack">
              <div className="detail-block">
                <h3>{selectedFile.fileName}</h3>
                <p>{selectedFile.absolutePath}</p>
                <p>
                  Status: <strong>{statusLabel(selectedFile.status, bootstrap)}</strong>
                </p>
                {selectedFile.failureMessage && <p className="error">{selectedFile.failureMessage}</p>}
              </div>

              <div className="detail-block">
                <h3>Extracted Text</h3>
                <textarea
                  readOnly
                  value={selectedFile.extractedDocument?.rawText ?? 'No extracted document'}
                />
              </div>

              <div className="detail-block">
                <h3>Mapped Payload</h3>
                <textarea
                  readOnly
                  value={prettyJson(selectedFile.mappedDocument?.mappedDataJson)}
                />
              </div>

              <div className="detail-split">
                <div className="detail-block">
                  <h3>Validation</h3>
                  <textarea
                    readOnly
                    value={prettyJson(selectedFile.mappedDocument?.validationIssuesJson)}
                  />
                </div>

                <div className="detail-block">
                  <h3>Match Results</h3>
                  <textarea
                    readOnly
                    value={prettyJson(selectedFile.mappedDocument?.matchResultsJson)}
                  />
                </div>
              </div>
            </div>
          )}
        </article>
      </section>

      <section className="panel">
        <h2>Approved Accounting Records</h2>
        <div className="records-table">
          <div className="table-head">
            <span>Invoice</span>
            <span>Counterparty</span>
            <span>Direction</span>
            <span>Total</span>
            <span>Category</span>
            <span>Status</span>
          </div>
          {savedDocuments.map((document) => (
            <div key={document.id} className="table-row">
              <span>{document.invoiceNumber}</span>
              <span>{document.counterpartyName}</span>
              <span>{directionLabel(document.direction)}</span>
              <span>
                {document.totalAmount?.toFixed(2) ?? '-'} {document.currency}
              </span>
              <span>{document.approvedCategory}</span>
              <span>{document.status}</span>
            </div>
          ))}
          {savedDocuments.length === 0 && <p>No approved accounting documents yet.</p>}
        </div>
      </section>
    </main>
  )
}

async function fetchJson<T>(url: string): Promise<T> {
  const response = await fetch(url)
  if (!response.ok) {
    throw new Error(`Request failed with ${response.status}`)
  }

  return (await response.json()) as T
}

async function postJson<T = unknown>(url: string, body?: unknown): Promise<T> {
  const response = await fetch(url, {
    method: 'POST',
    headers: body ? { 'Content-Type': 'application/json' } : undefined,
    body: body ? JSON.stringify(body) : '',
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(text || `Request failed with ${response.status}`)
  }

  return (await response.json()) as T
}

function prettyJson(json: string | undefined): string {
  if (!json) {
    return 'No data'
  }

  try {
    return JSON.stringify(JSON.parse(json), null, 2)
  } catch {
    return json
  }
}

function statusLabel(status: number, bootstrap: Bootstrap | null): string {
  return bootstrap?.fileStatuses[status] ?? `Status ${status}`
}

function directionLabel(direction: number): string {
  return direction === 2 ? 'Income' : direction === 1 ? 'Expense' : 'Unknown'
}

function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }

  return 'An unexpected error occurred.'
}

export default App
