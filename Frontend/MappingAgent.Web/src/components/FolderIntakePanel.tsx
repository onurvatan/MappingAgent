import type { FormEvent } from 'react'

type FolderIntakePanelProps = {
  folderPath: string
  onFolderPathChange: (value: string) => void
  onProcess: (event: FormEvent) => void
  busyAction: 'process' | null
  processLabel: string
  error: string | null
  demoFolder: string
}

export function FolderIntakePanel({
  folderPath,
  onFolderPathChange,
  onProcess,
  busyAction,
  processLabel,
  error,
  demoFolder,
}: FolderIntakePanelProps) {
  return (
    <section className="panel action-panel">
      <div className="panel-heading">
        <h2>Folder Intake</h2>
        <span className={`signal ${busyAction ? 'signal-live' : ''}`}>
          {busyAction ? 'processing' : 'ready'}
        </span>
      </div>

      <form className="folder-form" onSubmit={onProcess}>
        <label htmlFor="folderPath">Folder path</label>
        <input
          id="folderPath"
          value={folderPath}
          onChange={(event) => onFolderPathChange(event.target.value)}
          placeholder="C:\\Accounting\\Incoming"
        />

        <div className="button-row">
          <button type="submit" disabled={busyAction !== null}>
            {busyAction === 'process' ? 'Processing...' : 'Process Folder'}
          </button>
        </div>
      </form>

      <p className="hint">
        Demo path: <code>{demoFolder}</code>
      </p>

      <div className="callout">
        <strong>{processLabel}</strong>
        <span>Process runs scan, parse, and mapping in sequence. Use Review only for files that still need attention.</span>
      </div>

      {error && <p className="error-banner">{error}</p>}
    </section>
  )
}
