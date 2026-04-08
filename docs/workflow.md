# Workflow

## End-to-End Flow

1. User selects a folder in the UI.
2. Backend scans the folder recursively for supported file types.
3. UI shows discovered files with metadata before ingestion starts.
4. User starts ingestion.
5. Backend creates an ingestion job and queues all discovered files.
6. Each file moves through the ingestion pipeline.
7. UI updates status in near real time.
8. User opens completed or review-required items.
9. User edits mapped fields if needed.
10. User approves the document.
11. Approved data is saved into the final accounting store.

## Supported File Types

- `.pdf`
- `.xlsx`
- `.csv`
- `.txt`

Unsupported files are ignored during discovery and reported in the scan summary if needed.

## File Lifecycle

Each file transitions through these states:

- `Discovered`
- `Queued`
- `Parsing`
- `Classifying`
- `Extracting`
- `Matching`
- `NeedsReview`
- `Completed`
- `Failed`

## Processing Pipeline

### 1. Discovery

The scan step records:

- absolute path
- file name
- extension
- file size
- last modified timestamp
- content hash

### 2. Parsing

The parser converts each file into a normalized extraction payload:

- plain text
- table data when available
- parser warnings
- parser metadata

### 3. Classification

The classification step determines:

- document kind
- direction: income or expense
- confidence score

Possible document kinds:

- `ExpenseInvoice`
- `IncomeInvoice`
- `CreditNote`
- `UnknownAccountingDocument`

### 4. Field Extraction

The extraction step maps document content into a canonical accounting structure.

Target fields:

- invoice number
- invoice date
- due date
- currency
- subtotal
- tax amount
- total amount
- counterparty name
- counterparty tax identifier
- payment reference
- line items

### 5. Categorization

The categorization step proposes an accounting category.

Expense categories:

- `Rent`
- `Utilities`
- `Software`
- `OfficeSupplies`
- `Travel`
- `Marketing`
- `ProfessionalServices`
- `Tax`
- `OtherExpense`

Income categories:

- `ProductSales`
- `ServiceRevenue`
- `SubscriptionRevenue`
- `ConsultingRevenue`
- `OtherIncome`

### 6. Matching

The matching step checks the current database for:

- duplicate invoice number
- known counterparty
- previous ingestion of the same file hash
- existing category defaults for the matched counterparty

### 7. Review Outcome

Each processed file ends in one of these outcomes:

- `Completed`: sufficient confidence and validation passed
- `NeedsReview`: extraction or matching uncertainty requires user confirmation
- `Failed`: parsing, classification, or validation could not produce a usable record

## Failure Reasons

Use explicit failure reasons so the UI can explain what happened:

- `UnsupportedFormat`
- `UnreadableDocument`
- `UnknownDocumentType`
- `MissingInvoiceNumber`
- `MissingCounterparty`
- `MissingTotal`
- `DuplicateInvoice`
- `CounterpartyNotFound`
- `CategoryUndetermined`
- `ValidationFailed`

## UI Expectations

The UI should expose:

- folder scan summary
- file list with live status
- selected file details
- extracted raw content
- mapped invoice fields
- confidence and validation messages
- edit and approve actions
