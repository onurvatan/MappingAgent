import type { ActivityItem, AccountingDocument, IngestionJob } from '../types'
import { directionLabel, formatUtc } from '../utils'

type OperationsPageProps = {
  jobs: IngestionJob[]
  selectedJobId: string | null
  onSelectJob: (jobId: string) => void
  onRefresh: () => void
  activity: ActivityItem[]
  onClearActivity: () => void
  savedDocuments: AccountingDocument[]
}

export function OperationsPage({
  jobs,
  selectedJobId,
  onSelectJob,
  onRefresh,
  activity,
  onClearActivity,
  savedDocuments,
}: OperationsPageProps) {
  return (
    <>
      <section className="workspace-grid">
        <section className="panel">
          <div className="panel-heading">
            <h2>Jobs</h2>
            <button type="button" className="ghost-button" onClick={onRefresh}>
              Refresh
            </button>
          </div>
          <div className="job-list scroll-panel">
            {jobs.map((job) => (
              <button
                key={job.id}
                type="button"
                className={`job-card ${job.id === selectedJobId ? 'selected' : ''}`}
                onClick={() => onSelectJob(job.id)}
              >
                <div className="job-card-top">
                  <span className="job-status">{job.status}</span>
                  <span>{formatUtc(job.startedAtUtc)}</span>
                </div>
                <strong>{job.folderPath}</strong>
                <span>
                  {job.totalFileCount} files, {job.completedFileCount} completed, {job.needsReviewFileCount} review,
                  {' '} {job.failedFileCount} failed
                </span>
              </button>
            ))}
            {jobs.length === 0 && <p className="empty-state">No jobs yet.</p>}
          </div>
        </section>

        <section className="panel activity-panel">
          <div className="panel-heading">
            <h2>Logs</h2>
            <button type="button" className="ghost-button" onClick={onClearActivity}>
              Clear
            </button>
          </div>
          <div className="activity-list">
            {activity.map((item) => (
              <div key={item.id} className={`activity-item activity-${item.level}`}>
                <span className="activity-time">{item.timestamp}</span>
                <p>{item.message}</p>
              </div>
            ))}
            {activity.length === 0 && <p className="empty-state">No activity yet.</p>}
          </div>
        </section>
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
          {savedDocuments.length === 0 && <p className="empty-state">No approved accounting documents yet.</p>}
        </div>
      </section>
    </>
  )
}
