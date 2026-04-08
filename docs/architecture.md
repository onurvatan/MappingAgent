# Architecture

## High-Level Design

The demo should be built as a local web application with:

- ASP.NET Core backend
- Microsoft Agent Framework workflow orchestration
- NoSQL store for ingestion and intermediate results
- EF Core relational store for approved accounting documents
- web UI for scan, monitor, review, and saved records

This is a hybrid storage architecture by design.

## Backend Responsibilities

The backend owns:

- folder scanning
- parser execution
- agent workflow execution
- validation and matching
- persistence
- job progress updates to the UI

## Agent Structure

Follow the `AgentCommerce` pattern, but replace sales/customer-service workflows with ingestion-specific workflows.

### AccountingIngestionOrchestratorAgent

Coordinates the ingestion pipeline for a file and decides which processing step runs next.

### DocumentClassificationAgent

Determines:

- expense invoice
- income invoice
- credit note
- unknown accounting document

### FieldExtractionAgent

Maps normalized parsed content to the canonical accounting payload.

### CategorizationAgent

Suggests an accounting category using extracted content and counterparty defaults.

## Non-Agent Components

These should remain deterministic services, not LLM logic:

- folder scanning
- file parsing
- schema validation
- duplicate detection
- counterparty lookup
- final persistence

The agent should propose structure. Deterministic code should decide whether it is valid to save.

## Parser Layer

Recommended parser components:

- `PdfParser` using `UglyToad.PdfPig`
- `ExcelParser` using `ClosedXML`
- `CsvParser` using `CsvHelper`
- `TextParser` using native file I/O

All parsers should emit the same normalized parse result.

## Storage

### Ingestion Store

Use a document-oriented store for:

- ingestion jobs
- source files
- extracted documents
- mapped accounting documents
- review history

Candidate choice:

- MongoDB for simplest local demo setup

Current implementation baseline:

- an ingestion store abstraction with an in-memory implementation for local development
- MongoDB remains the intended upgrade path once live ingestion persistence is introduced

### Final Store

Use EF Core with SQLite for approved accounting records in the demo.

Reasons:

- low setup cost
- visible relational model
- easy local execution

## UI

Recommended pages:

- folder intake page
- ingestion monitor page
- review workspace page
- saved records page

Recommended UI behaviors:

- polling or SignalR for live progress
- status filters
- per-file detail panel
- editable mapped form
- approve and reject actions

## API Surface

Planned endpoints:

- `POST /api/folders/scan`
- `POST /api/ingestion-jobs`
- `GET /api/ingestion-jobs/{id}`
- `GET /api/ingestion-jobs/{id}/files`
- `GET /api/source-files/{id}`
- `POST /api/mapped-documents/{id}/approve`
- `POST /api/mapped-documents/{id}/reject`
- `PUT /api/mapped-documents/{id}`
- `GET /api/accounting-documents`

## Reference Alignment with AgentCommerce

Reuse these ideas from the guide project:

- DI-based agent registration
- focused tool classes
- workflow-oriented agent composition
- minimal API endpoints
- clear separation between workflow orchestration and deterministic services
