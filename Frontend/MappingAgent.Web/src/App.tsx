import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'
import { FileQueueTable } from './components/FileQueueTable'
import { FolderIntakePanel } from './components/FolderIntakePanel'
import { OperationsPage } from './components/OperationsPage'
import { PageNav } from './components/PageNav'
import { ReviewModal } from './components/ReviewModal'
import type { ActivityItem, AccountingDocument, Bootstrap, IngestionJob, SourceFile } from './types'

const demoFolder = 'c:\\Users\\Techp\\git\\MappingAgent\\DemoData\\Phase4Sample'
const pollingStatuses = new Set(['Parsing', 'Mapping'])

type PageKey = 'queue' | 'operations'

function App() {
  const [page, setPage] = useState<PageKey>('queue')
  const [bootstrap, setBootstrap] = useState<Bootstrap | null>(null)
  const [jobs, setJobs] = useState<IngestionJob[]>([])
  const [files, setFiles] = useState<SourceFile[]>([])
  const [savedDocuments, setSavedDocuments] = useState<AccountingDocument[]>([])
  const [folderPath, setFolderPath] = useState(demoFolder)
  const [selectedJobId, setSelectedJobId] = useState<string | null>(null)
  const [reviewFileId, setReviewFileId] = useState<string | null>(null)
  const [busyAction, setBusyAction] = useState<'process' | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [activity, setActivity] = useState<ActivityItem[]>([])

  useEffect(() => {
    void initialize()
  }, [])

  useEffect(() => {
    if (!selectedJobId) {
      setFiles([])
      return
    }

    void refreshFiles(selectedJobId)
  }, [selectedJobId])

  useEffect(() => {
    const currentJob = jobs.find((job) => job.id === selectedJobId) ?? null
    if (!currentJob || !pollingStatuses.has(currentJob.status)) {
      return
    }

    const interval = window.setInterval(() => {
      void refreshJobsAndDocuments(currentJob.id)
      void refreshFiles(currentJob.id)
    }, 2000)

    return () => window.clearInterval(interval)
  }, [jobs, selectedJobId])

  const selectedJob = useMemo(
    () => jobs.find((job) => job.id === selectedJobId) ?? null,
    [jobs, selectedJobId],
  )

  const reviewFile = useMemo(
    () => files.find((file) => file.id === reviewFileId) ?? null,
    [files, reviewFileId],
  )

  const processLabel = useMemo(() => {
    if (!selectedJob) {
      return 'Create a queue from a folder.'
    }

    if (files.length === 0) {
      return 'Selected job has no supported files.'
    }

    const needsReview = files.filter((file) => file.status === 6).length
    const completed = files.filter((file) => file.status === 7).length
    const failed = files.filter((file) => file.status === 8).length

    return `${completed} completed, ${needsReview} need review, ${failed} failed.`
  }, [files, selectedJob])

  async function initialize() {
    try {
      pushActivity('info', 'Loading queue, jobs, and approved records.')
      const [bootstrapResponse, jobsResponse, accountingDocuments] = await Promise.all([
        fetchJson<Bootstrap>('/api/meta/bootstrap'),
        fetchJson<IngestionJob[]>('/api/ingestion-jobs'),
        fetchJson<AccountingDocument[]>('/api/accounting-documents'),
      ])

      setBootstrap(bootstrapResponse)
      setJobs(jobsResponse)
      setSavedDocuments(accountingDocuments)
      setSelectedJobId(jobsResponse[0]?.id ?? null)
      pushActivity('success', `Loaded ${jobsResponse.length} jobs.`)
    } catch (fetchError) {
      const message = getErrorMessage(fetchError)
      setError(message)
      pushActivity('error', `Initial load failed: ${message}`)
    }
  }

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
    try {
      const response = await fetchJson<SourceFile[]>(`/api/ingestion-jobs/${jobId}/files`)
      setFiles(response)
      setReviewFileId((current) => (current && response.some((file) => file.id === current) ? current : null))
    } catch (fetchError) {
      const message = getErrorMessage(fetchError)
      setError(message)
      pushActivity('error', `Failed to load files: ${message}`)
    }
  }

  async function handleProcess(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setBusyAction('process')
    pushActivity('info', `Processing ${folderPath}`)

    try {
      const scanResponse = await postJson<{ ingestionJobId: string }>('/api/folders/scan', { folderPath })
      await refreshJobsAndDocuments(scanResponse.ingestionJobId)
      await refreshFiles(scanResponse.ingestionJobId)
      pushActivity('success', 'Scan completed.')

      const parseResponse = await postJson<{ parsedFiles: number; failedFiles: number }>(
        `/api/ingestion-jobs/${scanResponse.ingestionJobId}/parse`,
      )

      await refreshJobsAndDocuments(scanResponse.ingestionJobId)
      await refreshFiles(scanResponse.ingestionJobId)
      pushActivity(
        parseResponse.failedFiles > 0 ? 'warning' : 'success',
        `Parse finished. Parsed ${parseResponse.parsedFiles}, failed ${parseResponse.failedFiles}.`,
      )

      const mapResponse = await postJson<{ completedFiles: number; needsReviewFiles: number; failedFiles: number }>(
        `/api/ingestion-jobs/${scanResponse.ingestionJobId}/map`,
      )

      await refreshJobsAndDocuments(scanResponse.ingestionJobId)
      await refreshFiles(scanResponse.ingestionJobId)
      pushActivity(
        mapResponse.failedFiles > 0 || mapResponse.needsReviewFiles > 0 ? 'warning' : 'success',
        `Mapping finished. Completed ${mapResponse.completedFiles}, review ${mapResponse.needsReviewFiles}, failed ${mapResponse.failedFiles}.`,
      )
    } catch (processError) {
      const message = getErrorMessage(processError)
      setError(message)
      pushActivity('error', `Processing failed: ${message}`)
    } finally {
      setBusyAction(null)
    }
  }

  function pushActivity(level: ActivityItem['level'], message: string) {
    setActivity((current) => [
      {
        id: `${Date.now()}-${Math.random().toString(16).slice(2)}`,
        timestamp: new Date().toLocaleTimeString(),
        level,
        message,
      },
      ...current,
    ].slice(0, 14))
  }

  return (
    <main className="app-shell">
      <header className="hero hero-simple">
        <div className="hero-copy">
          <p className="eyebrow">MappingAgent</p>
          <h1>Accounting Queue</h1>
          <p className="lede">Process files from a folder, then review only when you need to.</p>
        </div>
        <PageNav activePage={page} onSelectPage={setPage} />
      </header>

      {page === 'queue' ? (
        <section className="queue-layout">
          <FolderIntakePanel
            folderPath={folderPath}
            onFolderPathChange={setFolderPath}
            onProcess={handleProcess}
            busyAction={busyAction}
            processLabel={processLabel}
            error={error}
            demoFolder={demoFolder}
          />

          <section className="panel">
            <div className="panel-heading">
              <h2>Files</h2>
              <span className="panel-meta">
                {selectedJob ? `${files.length} files in selected job` : 'No job selected'}
              </span>
            </div>
            <FileQueueTable
              files={files}
              bootstrap={bootstrap}
              onReview={(file) => {
                setReviewFileId(file.id)
                pushActivity('info', `Opened review for ${file.fileName}`)
              }}
            />
          </section>
        </section>
      ) : (
        <OperationsPage
          jobs={jobs}
          selectedJobId={selectedJobId}
          onSelectJob={setSelectedJobId}
          onRefresh={() => void refreshJobsAndDocuments(selectedJobId ?? undefined)}
          activity={activity}
          onClearActivity={() => setActivity([])}
          savedDocuments={savedDocuments}
        />
      )}

      <ReviewModal
        file={reviewFile}
        bootstrap={bootstrap}
        onClose={() => setReviewFileId(null)}
      />
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

function getErrorMessage(error: unknown): string {
  if (error instanceof Error) {
    return error.message
  }

  return 'An unexpected error occurred.'
}

export default App
