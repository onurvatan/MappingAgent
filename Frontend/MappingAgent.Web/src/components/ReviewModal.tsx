import { useEffect, useMemo, useState } from 'react'
import type { Bootstrap, EditableMappedPayload, SourceFile } from '../types'
import { buildEditablePayload, parseArray, parseJson, parseNumericField, prettyJson, statusLabel } from '../utils'

type ReviewModalProps = {
  file: SourceFile | null
  bootstrap: Bootstrap | null
  onClose: () => void
}

export function ReviewModal({ file, bootstrap, onClose }: ReviewModalProps) {
  const [editedPayload, setEditedPayload] = useState<EditableMappedPayload | null>(null)

  useEffect(() => {
    setEditedPayload(buildEditablePayload(file))
  }, [file])

  const validationIssues = useMemo(
    () => parseArray(file?.mappedDocument?.validationIssuesJson),
    [file],
  )

  const parserWarnings = useMemo(
    () => parseArray(file?.extractedDocument?.parserWarningsJson),
    [file],
  )

  const matchResults = useMemo(
    () => parseJson(file?.mappedDocument?.matchResultsJson),
    [file],
  )

  const categoryOptions = useMemo(() => {
    if (!bootstrap || !editedPayload) {
      return []
    }

    return editedPayload.direction === 'Income'
      ? bootstrap.incomeCategories
      : bootstrap.expenseCategories
  }, [bootstrap, editedPayload])

  const payloadPreview = useMemo(() => {
    if (!editedPayload) {
      return 'No mapped payload'
    }

    return JSON.stringify(
      {
        documentKind: editedPayload.documentKind || null,
        direction: editedPayload.direction || null,
        counterparty: {
          counterpartyName: editedPayload.counterpartyName || null,
          counterpartyTaxIdentifier: editedPayload.counterpartyTaxIdentifier || null,
        },
        invoice: {
          invoiceNumber: editedPayload.invoiceNumber || null,
          invoiceDate: editedPayload.invoiceDate || null,
          dueDate: editedPayload.dueDate || null,
          currency: editedPayload.currency || null,
          subtotal: parseNumericField(editedPayload.subtotal),
          taxAmount: parseNumericField(editedPayload.taxAmount),
          totalAmount: parseNumericField(editedPayload.totalAmount),
        },
        category: editedPayload.category || null,
      },
      null,
      2,
    )
  }, [editedPayload])

  if (!file) {
    return null
  }

  return (
    <div className="modal-backdrop" onClick={onClose}>
      <section className="modal-card" onClick={(event) => event.stopPropagation()}>
        <div className="panel-heading">
          <div>
            <h2>Review File</h2>
            <p className="panel-meta">{file.fileName}</p>
          </div>
          <div className="status-stack">
            <span className={`badge badge-status badge-${file.status}`}>
              {statusLabel(file.status, bootstrap)}
            </span>
            <button type="button" className="ghost-button" onClick={onClose}>
              Close
            </button>
          </div>
        </div>

        {file.failureMessage && (
          <div className="error-block">
            <h3>Failure</h3>
            <p>{file.failureReason}</p>
            <p>{file.failureMessage}</p>
          </div>
        )}

        <div className="detail-grid">
          <div className="detail-block">
            <h3>Extracted Text</h3>
            <textarea
              className="review-textarea review-textarea-source"
              readOnly
              value={file.extractedDocument?.rawText ?? 'No extracted document. Parse this job first.'}
            />
          </div>

          <div className="detail-block">
            <h3>Editable Fields</h3>
            {!editedPayload && <p className="empty-state">No mapped payload available yet.</p>}
            {editedPayload && (
              <div className="editor-grid">
                <label>
                  <span>Document Kind</span>
                  <input
                    value={editedPayload.documentKind}
                    onChange={(event) => setEditedPayload({ ...editedPayload, documentKind: event.target.value })}
                  />
                </label>
                <label>
                  <span>Direction</span>
                  <select
                    value={editedPayload.direction}
                    onChange={(event) => setEditedPayload({ ...editedPayload, direction: event.target.value })}
                  >
                    <option value="Unknown">Unknown</option>
                    <option value="Expense">Expense</option>
                    <option value="Income">Income</option>
                  </select>
                </label>
                <label>
                  <span>Counterparty</span>
                  <input
                    value={editedPayload.counterpartyName}
                    onChange={(event) => setEditedPayload({ ...editedPayload, counterpartyName: event.target.value })}
                  />
                </label>
                <label>
                  <span>Invoice Number</span>
                  <input
                    value={editedPayload.invoiceNumber}
                    onChange={(event) => setEditedPayload({ ...editedPayload, invoiceNumber: event.target.value })}
                  />
                </label>
                <label>
                  <span>Invoice Date</span>
                  <input
                    value={editedPayload.invoiceDate}
                    onChange={(event) => setEditedPayload({ ...editedPayload, invoiceDate: event.target.value })}
                  />
                </label>
                <label>
                  <span>Due Date</span>
                  <input
                    value={editedPayload.dueDate}
                    onChange={(event) => setEditedPayload({ ...editedPayload, dueDate: event.target.value })}
                  />
                </label>
                <label>
                  <span>Currency</span>
                  <input
                    value={editedPayload.currency}
                    onChange={(event) => setEditedPayload({ ...editedPayload, currency: event.target.value })}
                  />
                </label>
                <label>
                  <span>Total</span>
                  <input
                    value={editedPayload.totalAmount}
                    onChange={(event) => setEditedPayload({ ...editedPayload, totalAmount: event.target.value })}
                  />
                </label>
                <label className="editor-grid-span">
                  <span>Category</span>
                  <select
                    value={editedPayload.category}
                    onChange={(event) => setEditedPayload({ ...editedPayload, category: event.target.value })}
                  >
                    <option value="">Select category</option>
                    {categoryOptions.map((option) => (
                      <option key={option} value={option}>
                        {option}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
            )}
          </div>
        </div>

        <div className="detail-split">
          <div className="detail-block">
            <h3>Validation Issues</h3>
            {validationIssues.length === 0 ? (
              <p className="empty-state">No validation issues.</p>
            ) : (
              <ul className="bullet-list">
                {validationIssues.map((issue, index) => (
                  <li key={`${issue}-${index}`}>{issue}</li>
                ))}
              </ul>
            )}
          </div>

          <div className="detail-block">
            <h3>Parser Warnings</h3>
            {parserWarnings.length === 0 ? (
              <p className="empty-state">No parser warnings.</p>
            ) : (
              <ul className="bullet-list">
                {parserWarnings.map((warning, index) => (
                  <li key={`${warning}-${index}`}>{warning}</li>
                ))}
              </ul>
            )}
          </div>
        </div>

        <div className="detail-split">
          <div className="detail-block">
            <h3>Payload Preview</h3>
            <textarea className="review-textarea review-textarea-json" readOnly value={payloadPreview} />
          </div>

          <div className="detail-block">
            <h3>Match Results</h3>
            <textarea className="review-textarea review-textarea-json" readOnly value={prettyJson(matchResults)} />
          </div>
        </div>
      </section>
    </div>
  )
}
