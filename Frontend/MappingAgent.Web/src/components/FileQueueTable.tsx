import type { Bootstrap, SourceFile } from '../types'
import { formatBytes } from '../utils'
import { StatusBadge } from './StatusBadge'

type FileQueueTableProps = {
  files: SourceFile[]
  bootstrap: Bootstrap | null
  onReview: (file: SourceFile) => void
}

export function FileQueueTable({ files, bootstrap, onReview }: FileQueueTableProps) {
  if (files.length === 0) {
    return <p className="empty-state">No files available in this queue.</p>
  }

  return (
    <div className="records-table">
      <div className="table-head file-queue-head">
        <span>File</span>
        <span>Status</span>
        <span>Type</span>
        <span>Actions</span>
      </div>
      {files.map((file) => (
        <div key={file.id} className="table-row file-queue-row">
          <span className="queue-file-name">{file.fileName}</span>
          <StatusBadge status={file.status} bootstrap={bootstrap} />
          <span>
            {file.extension} · {formatBytes(file.sizeBytes)}
          </span>
          <div className="row-actions">
            {isReviewAvailable(file) ? (
              <button type="button" className="ghost-button" onClick={() => onReview(file)}>
                Review
              </button>
            ) : (
              <span className="action-label">No action</span>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}

function isReviewAvailable(file: SourceFile): boolean {
  const reviewStatus = file.mappedDocument?.reviewStatus
  return reviewStatus !== 2
}
