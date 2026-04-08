import { useEffect, useState } from 'react'
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

function App() {
  const [bootstrap, setBootstrap] = useState<Bootstrap | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    void fetch('/api/meta/bootstrap')
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`Bootstrap request failed with ${response.status}`)
        }

        return (await response.json()) as Bootstrap
      })
      .then(setBootstrap)
      .catch((fetchError: Error) => {
        setError(fetchError.message)
      })
  }, [])

  return (
    <main className="app-shell">
      <section className="hero">
        <p className="eyebrow">Phase 1 Baseline</p>
        <h1>Accounting document ingestion workspace</h1>
        <p className="lede">
          This starter shell aligns the repo with the docs: local folder scan,
          mixed file discovery, accounting classification, review workflow, and
          hybrid storage.
        </p>
      </section>

      <section className="panel-grid">
        <article className="panel panel-accent">
          <h2>Planned flow</h2>
          <ol>
            <li>Select folder</li>
            <li>Scan supported files</li>
            <li>Start ingestion job</li>
            <li>Track per-file status</li>
            <li>Review and edit mapped fields</li>
            <li>Approve into final accounting store</li>
          </ol>
        </article>

        <article className="panel">
          <h2>Backend bootstrap</h2>
          {error && <p className="error">{error}</p>}
          {!error && !bootstrap && <p>Loading API bootstrap...</p>}
          {bootstrap && (
            <dl className="facts">
              <div>
                <dt>App</dt>
                <dd>{bootstrap.applicationName}</dd>
              </div>
              <div>
                <dt>Version</dt>
                <dd>{bootstrap.apiVersion}</dd>
              </div>
              <div>
                <dt>Extensions</dt>
                <dd>{bootstrap.supportedExtensions.join(', ')}</dd>
              </div>
            </dl>
          )}
        </article>
      </section>

      <section className="panel-grid">
        <article className="panel">
          <h2>Document kinds</h2>
          <ul className="tag-list">
            {(bootstrap?.documentKinds ?? []).map((kind) => (
              <li key={kind}>{kind}</li>
            ))}
          </ul>
        </article>

        <article className="panel">
          <h2>File statuses</h2>
          <ul className="tag-list">
            {(bootstrap?.fileStatuses ?? []).map((status) => (
              <li key={status}>{status}</li>
            ))}
          </ul>
        </article>
      </section>

      <section className="panel-grid">
        <article className="panel">
          <h2>Expense categories</h2>
          <ul className="tag-list">
            {(bootstrap?.expenseCategories ?? []).map((category) => (
              <li key={category}>{category}</li>
            ))}
          </ul>
        </article>

        <article className="panel">
          <h2>Income categories</h2>
          <ul className="tag-list">
            {(bootstrap?.incomeCategories ?? []).map((category) => (
              <li key={category}>{category}</li>
            ))}
          </ul>
        </article>
      </section>
    </main>
  )
}

export default App
