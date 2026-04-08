# Domain Model

## Modeling Strategy

Use two storage layers:

1. ingestion storage for raw and intermediate results
2. final accounting storage for approved records

This avoids forcing first-pass AI output directly into final accounting entities.

## Ingestion Layer

### IngestionJob

Represents a batch started from one folder scan.

Fields:

- `Id`
- `FolderPath`
- `Status`
- `StartedAtUtc`
- `CompletedAtUtc`
- `TotalFileCount`
- `CompletedFileCount`
- `FailedFileCount`
- `NeedsReviewFileCount`

### SourceFile

Represents one discovered file inside an ingestion job.

Fields:

- `Id`
- `IngestionJobId`
- `AbsolutePath`
- `FileName`
- `Extension`
- `SizeBytes`
- `LastModifiedUtc`
- `ContentHash`
- `Status`
- `FailureReason`
- `FailureMessage`

### ExtractedDocument

Represents parsed content independent of final mapping.

Fields:

- `Id`
- `SourceFileId`
- `RawText`
- `TablesJson`
- `ParserName`
- `ParserWarningsJson`

### MappedAccountingDocument

Represents the system's proposed structured accounting interpretation.

Fields:

- `Id`
- `SourceFileId`
- `DocumentKind`
- `Direction`
- `SuggestedCategory`
- `ConfidenceScore`
- `MappedDataJson`
- `ValidationIssuesJson`
- `MatchResultsJson`
- `ReviewStatus`

## Final Accounting Layer

### Counterparty

Represents a supplier or customer.

Fields:

- `Id`
- `Name`
- `Type`
- `TaxIdentifier`
- `Email`
- `DefaultCategory`

Counterparty type:

- `Vendor`
- `Customer`

### AccountingDocument

Represents the approved invoice-like record.

Fields:

- `Id`
- `SourceFileId`
- `CounterpartyId`
- `DocumentKind`
- `Direction`
- `InvoiceNumber`
- `InvoiceDate`
- `DueDate`
- `Currency`
- `Subtotal`
- `TaxAmount`
- `TotalAmount`
- `SuggestedCategory`
- `ApprovedCategory`
- `Status`
- `ConfidenceScore`
- `ApprovedBy`
- `ApprovedAtUtc`

### AccountingDocumentLine

Represents extracted line items.

Fields:

- `Id`
- `AccountingDocumentId`
- `Description`
- `Quantity`
- `UnitPrice`
- `LineAmount`
- `TaxRate`

## Enumerations

### DocumentKind

- `ExpenseInvoice`
- `IncomeInvoice`
- `CreditNote`
- `UnknownAccountingDocument`

### DocumentDirection

- `Expense`
- `Income`
- `Unknown`

### ReviewStatus

- `Pending`
- `NeedsReview`
- `Approved`
- `Rejected`

### FileProcessingStatus

- `Discovered`
- `Queued`
- `Parsing`
- `Classifying`
- `Extracting`
- `Matching`
- `NeedsReview`
- `Completed`
- `Failed`

## Validation Rules

Minimum validation for an invoice-like record:

- invoice number should exist
- counterparty should exist or be creatable
- invoice date should be parseable
- total amount should exist
- subtotal + tax should reconcile with total when values are present
- duplicate invoice number for the same counterparty should be flagged

## Canonical Mapping Payload

The intermediate mapped payload should be JSON-shaped and stable across file types.

Example fields:

- `documentId`
- `documentKind`
- `direction`
- `counterparty`
- `invoice`
- `lineItems`
- `category`
- `confidence`
- `warnings`

This payload is the contract between parsing, agent mapping, UI review, and final persistence.
